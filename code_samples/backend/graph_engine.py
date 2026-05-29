import networkx as nx
import sqlite3
import os
import math

def haversine(lat1, lon1, lat2, lon2):
    """
    根据经纬度计算两点之间的真实距离（单位：米）
    """
    R = 6371000  # 地球半径（米）
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lon2 - lon1)
    
    a = math.sin(delta_phi / 2.0) ** 2 + \
        math.cos(phi1) * math.cos(phi2) * \
        math.sin(delta_lambda / 2.0) ** 2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return R * c

def calculate_bearing(lat1, lon1, lat2, lon2):
    """
    计算从点1到点2的初始方位角（以正北为0度，顺时针方向）
    """
    lat1 = math.radians(lat1)
    lat2 = math.radians(lat2)
    diff_lon = math.radians(lon2 - lon1)
    
    x = math.sin(diff_lon) * math.cos(lat2)
    y = math.cos(lat1) * math.sin(lat2) - (math.sin(lat1) * math.cos(lat2) * math.cos(diff_lon))
    
    initial_bearing = math.atan2(x, y)
    initial_bearing = math.degrees(initial_bearing)
    compass_bearing = (initial_bearing + 360) % 360
    return compass_bearing

def calculate_edge_weight(distance, slope, friction, user_type):
    profiles = {
        "normal":   {"slope_k":3,  "friction_k":5,  "dist_k":1.0},
        "elderly":  {"slope_k":8,  "friction_k":12, "dist_k":1.2},
        "disabled": {"slope_k":20, "friction_k":20, "dist_k":1.5},
        "blind":    {"slope_k":15, "friction_k":25, "dist_k":1.3},
    }
    p = profiles.get(user_type, profiles["normal"])
    return (distance * p["dist_k"] 
            + (slope**2) * p["slope_k"] 
            + (1.0/max(friction,0.1)) * p["friction_k"])

def get_direction_description(bearing):
    """
    将方位角转换为自然语言的绝对方向
    """
    dirs = ["北", "东北", "东", "东南", "南", "西南", "西", "西北"]
    ix = round(bearing / (360. / 8))
    return dirs[ix % 8]

def get_relative_turn(user_bearing, target_bearing):
    """
    基于用户当前行进方向的精细转向指令
    """
    diff = (target_bearing - user_bearing + 360) % 360
    if diff < 22.5 or diff > 337.5:
        return "直行"
    elif 22.5 <= diff <= 67.5:
        return "轻微右转（约45度）"
    elif 67.5 < diff <= 112.5:
        return "右转"
    elif 112.5 < diff <= 157.5:
        return "大幅右转"
    elif 157.5 < diff <= 202.5:
        return "掉头"
    elif 202.5 < diff <= 247.5:
        return "大幅左转"
    elif 247.5 < diff <= 292.5:
        return "左转"
    else:
        return "轻微左转（约45度）"

def get_turn_instruction(current_bearing, next_bearing):
    """
    根据当前行进方向和下一个路段方向，计算转向指令
    """
    diff = (next_bearing - current_bearing + 360) % 360
    if diff < 30 or diff > 330:
        return "直行"
    elif 30 <= diff <= 150:
        return "右转"
    elif 150 < diff < 210:
        return "掉头"
    else:
        return "左转"

class RouteEngine:
    def __init__(self):
        self.G = nx.Graph()
        self.db_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "gulangyu.db")
        self.reload_graph()

    def reload_graph(self):
        """
        从本地数据库动态读取无障碍节点和路径，构建拓扑图
        """
        self.G.clear()
        if not os.path.exists(self.db_path):
            print("数据库不存在，无法构建图")
            return

        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()

        # 1. 读取所有节点 (Nodes)
        cursor.execute("SELECT node_id, node_type, name, latitude, longitude, audio_ambient, smell, detail FROM accessibility_nodes")
        nodes = cursor.fetchall()
        
        node_dict = {}
        for node in nodes:
            node_id = node['node_id']
            # 将节点加入图中，附带属性
            self.G.add_node(
                node_id, 
                name=node['name'], 
                lat=node['latitude'], 
                lng=node['longitude'],
                detail=node['detail'],
                node_type=node['node_type']
            )
            node_dict[node_id] = node

        # 2. 读取所有路段 (Edges)
        cursor.execute("SELECT edge_id, start_node, end_node, material, width_m, slope_deg, friction, note FROM roads")
        edges = cursor.fetchall()

        valid_edges = []
        for edge in edges:
            u = edge['start_node']
            v = edge['end_node']
            
            if u not in node_dict or v not in node_dict:
                continue
            
            valid_edges.append(edge)

            # 使用 Haversine 公式计算真实距离
            lat1, lon1 = node_dict[u]['latitude'], node_dict[u]['longitude']
            lat2, lon2 = node_dict[v]['latitude'], node_dict[v]['longitude']
            distance = haversine(lat1, lon1, lat2, lon2)

            # 提取路况参数
            slope = edge['slope_deg'] or 0
            friction = edge['friction'] or 1.0
            
            # 将原始参数存入图中，便于后续动态计算
            self.G.add_edge(
                u, v, 
                distance=round(distance, 2), 
                slope=slope,
                friction=friction,
                material=edge['material'],
                note=edge['note'],
                weight=calculate_edge_weight(distance, slope, friction, "normal")
            )

        # 3. 动态将“建筑入口”连接到最近的路段 (实现建筑到路口的寻路)
        building_nodes = [n for n in node_dict.values() if n['node_type'] == '建筑入口']
        for b_node in building_nodes:
            b_id = b_node['node_id']
            b_lat, b_lon = b_node['latitude'], b_node['longitude']
            
            best_edge = None
            min_deviation = float('inf')
            best_u, best_v = None, None
            best_d_u, best_d_v = 0, 0
            
            for edge in valid_edges:
                u, v = edge['start_node'], edge['end_node']
                u_lat, u_lon = node_dict[u]['latitude'], node_dict[u]['longitude']
                v_lat, v_lon = node_dict[v]['latitude'], node_dict[v]['longitude']
                
                d_u = haversine(b_lat, b_lon, u_lat, u_lon)
                d_v = haversine(b_lat, b_lon, v_lat, v_lon)
                d_edge = haversine(u_lat, u_lon, v_lat, v_lon)
                
                # 偏差值：计算建筑偏离这段路的距离
                deviation = d_u + d_v - d_edge
                if deviation < min_deviation:
                    min_deviation = deviation
                    best_edge = edge
                    best_u, best_v = u, v
                    best_d_u, best_d_v = d_u, d_v
                    
            if best_edge:
                # 找到了建筑所在的边，将其分别连接到两端路口
                slope = best_edge['slope_deg'] or 0
                friction = best_edge['friction'] or 1.0
                
                # 边: 建筑 -> 路口A
                self.G.add_edge(b_id, best_u, 
                    distance=round(best_d_u, 2),
                    slope=slope,
                    friction=friction,
                    material=best_edge['material'],
                    note=f"从建筑到路口",
                    weight=calculate_edge_weight(best_d_u, slope, friction, "normal")
                )
                # 边: 建筑 -> 路口B
                self.G.add_edge(b_id, best_v, 
                    distance=round(best_d_v, 2),
                    slope=slope,
                    friction=friction,
                    material=best_edge['material'],
                    note=f"从建筑到路口",
                    weight=calculate_edge_weight(best_d_v, slope, friction, "normal")
                )

        conn.close()
        print(f"图数据加载完成: {self.G.number_of_nodes()} 个节点, {self.G.number_of_edges()} 条边")

    def get_safest_route(self, start_node_id, end_node_id, user_type="normal", user_bearing=None, max_routes=3, strategy="safest"):
        """
        计算最安全路径 (支持差异化多路径)
        """
        try:
            if start_node_id not in self.G or end_node_id not in self.G:
                return {
                    "error": f"节点不存在。请检查输入 ID",
                    "available_nodes": list(self.G.nodes)
                }

            strategy = (strategy or "safest").lower()
            if strategy not in {"safest", "shortest"}:
                strategy = "safest"

            def weight_func(u, v, d):
                if strategy == "shortest":
                    return d.get('distance', 0)
                return calculate_edge_weight(d.get('distance', 0), d.get('slope', 0), d.get('friction', 1.0), user_type)

            # 获取按权重排序的候选路径
            candidate_paths = nx.shortest_simple_paths(self.G, start_node_id, end_node_id, weight=weight_func)
            
            final_routes = []
            selected_paths = []
            
            speed_map = {"normal": 80, "elderly": 50, "disabled": 40, "blind": 45}
            speed = speed_map.get(user_type, 80)

            for path in candidate_paths:
                if len(final_routes) >= max_routes:
                    break
                
                # 计算与已选路径的重叠率
                path_edges = set(zip(path[:-1], path[1:]))
                is_too_similar = False
                
                for sp in selected_paths:
                    sp_edges = set(zip(sp[:-1], sp[1:]))
                    overlap = len(path_edges & sp_edges) + len(path_edges & {(v, u) for u, v in sp_edges})
                    shorter_len = min(len(path_edges), len(sp_edges))
                    if shorter_len > 0 and overlap / shorter_len >= 0.5:
                        is_too_similar = True
                        break
                        
                if is_too_similar and final_routes:
                    continue
                    
                selected_paths.append(path)
                
                # 计算路径详情
                total_distance = 0
                total_obstacle_cost = 0
                steps = []
                current_bearing = user_bearing

                for i in range(len(path) - 1):
                    u = path[i]
                    v = path[i+1]
                    edge_data = self.G[u][v]
                    
                    u_lat, u_lng = self.G.nodes[u]['lat'], self.G.nodes[u]['lng']
                    v_lat, v_lng = self.G.nodes[v]['lat'], self.G.nodes[v]['lng']
                    
                    next_bearing = calculate_bearing(u_lat, u_lng, v_lat, v_lng)
                    absolute_direction = f"向{get_direction_description(next_bearing)}"
                    
                    if i == 0:
                        if current_bearing is None:
                            turn_instruction = "出发"
                        else:
                            turn_instruction = get_relative_turn(current_bearing, next_bearing)
                    else:
                        turn_instruction = get_turn_instruction(current_bearing, next_bearing)
                    
                    current_bearing = next_bearing
                    
                    dist = edge_data.get('distance', 0)
                    slope = edge_data.get('slope', 0)
                    friction = edge_data.get('friction', 1.0)
                    
                    cost = (slope**2) * 5 + (1.0/max(friction, 0.1)) * 10
                    
                    total_distance += dist
                    total_obstacle_cost += cost
                    
                    steps.append({
                        "from_name": self.G.nodes[u]['name'],
                        "to_name": self.G.nodes[v]['name'],
                        "distance": dist,
                        "material": edge_data.get('material', ''),
                        "turn_instruction": turn_instruction,
                        "absolute_direction": absolute_direction,
                        "note": edge_data.get('note', '')
                    })
                
                # 计算难度得分 0-10
                difficulty = min(10.0, total_obstacle_cost / (total_distance * 0.1 + 1))
                
                # 生成路线摘要
                summary_nodes = [steps[0]['from_name']]
                for i in range(1, len(steps)):
                    if i % max(1, len(steps)//3) == 0:
                        summary_nodes.append(steps[i]['from_name'])
                summary_nodes.append(steps[-1]['to_name'])
                route_summary = "经由" + "→".join(summary_nodes)

                final_routes.append({
                    "route_index": len(final_routes) + 1,
                    "total_distance": round(total_distance, 2),
                    "estimated_minutes": round(total_distance / speed),
                    "difficulty_score": round(difficulty, 1),
                    "route_summary": route_summary,
                    "steps": steps
                })

            if not final_routes:
                return {"success": False, "error": "无法到达目标节点（无路可走）"}

            return {
                "success": True,
                "routes": final_routes,
                "recommended": 0,
                "strategy": strategy,
                "start_name": self.G.nodes[start_node_id]['name'],
                "end_name": self.G.nodes[end_node_id]['name']
            }

        except nx.NetworkXNoPath:
            return {"success": False, "error": "无法到达目标节点（无路可走）"}
        except Exception as e:
            return {"success": False, "error": str(e)}

# 创建全局单例
route_engine = RouteEngine()
