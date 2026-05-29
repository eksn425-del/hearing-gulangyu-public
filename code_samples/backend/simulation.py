import pandas as pd
import networkx as nx
import random
import time
from graph_engine import route_engine

def calculate_path_metrics(G, path):
    """
    计算路径的总距离和总障碍惩罚
    """
    total_distance = 0
    total_obstacle_cost = 0
    has_high_risk = False
    
    for i in range(len(path) - 1):
        u = path[i]
        v = path[i+1]
        edge_data = G[u][v]
        
        dist = edge_data['distance']
        cost = edge_data['obstacle_cost']
        
        total_distance += dist
        total_obstacle_cost += cost
        
        if cost >= 50:  # 假设障碍惩罚大于等于50即为有风险（如台阶、陡坡）
            has_high_risk = True
            
    return total_distance, total_obstacle_cost, has_high_risk

def run_simulation():
    print("🚀 开始执行盲人寻路算法仿真测试...")
    
    # 1. 获取所有节点
    nodes = list(route_engine.G.nodes())
    
    if len(nodes) < 2:
        print("❌ 节点数量不足，无法进行仿真")
        return

    results = []
    num_simulations = 1000
    
    print(f"📊 正在生成 {num_simulations} 次随机寻路请求...")
    
    for i in range(num_simulations):
        # 随机选择起点和终点 (确保不相同)
        start_node = random.choice(nodes)
        end_node = random.choice(nodes)
        while start_node == end_node:
            end_node = random.choice(nodes)
            
        # ---------------------------------------------------------
        # 方案A：普通地图逻辑 (只看 distance 最短)
        # ---------------------------------------------------------
        try:
            path_a = nx.dijkstra_path(route_engine.G, start_node, end_node, weight='distance')
            dist_a, cost_a, risk_a = calculate_path_metrics(route_engine.G, path_a)
        except nx.NetworkXNoPath:
            continue # 如果无路可走则跳过

        # ---------------------------------------------------------
        # 方案B：盲人关怀逻辑 (看 distance + obstacle_cost 最安全)
        # ---------------------------------------------------------
        try:
            path_b = nx.dijkstra_path(route_engine.G, start_node, end_node, weight='weight')
            dist_b, cost_b, risk_b = calculate_path_metrics(route_engine.G, path_b)
        except nx.NetworkXNoPath:
            continue

        results.append({
            "simulation_id": i + 1,
            "start_node": start_node,
            "end_node": end_node,
            # 方案A数据
            "path_A_distance": dist_a,
            "path_A_obstacle_cost": cost_a,
            "path_A_has_risk": risk_a,
            # 方案B数据
            "path_B_distance": dist_b,
            "path_B_obstacle_cost": cost_b,
            "path_B_has_risk": risk_b,
            # 对比
            "distance_increase": dist_b - dist_a,
            "safety_improved": cost_a > cost_b
        })

    # 2. 生成 Pandas DataFrame
    df = pd.DataFrame(results)
    
    # 3. 计算统计摘要
    total_requests = len(df)
    
    # 方案A 遇到高风险的次数
    risk_count_A = df['path_A_has_risk'].sum()
    
    # 方案B 遇到高风险的次数
    risk_count_B = df['path_B_has_risk'].sum()
    
    # 方案B 成功避开危险的次数 (即 A 有风险 但 B 无风险)
    avoided_danger_count = len(df[(df['path_A_has_risk'] == True) & (df['path_B_has_risk'] == False)])
    
    # 方案B 成功避险比例 (相对于 A 遇到的所有风险)
    avoidance_rate = (avoided_danger_count / risk_count_A * 100) if risk_count_A > 0 else 0
    
    # 4. 输出结果到控制台
    print("\n" + "="*50)
    print("📄 仿真结果摘要 (Simulation Summary)")
    print("="*50)
    print(f"总测试请求次数: {total_requests}")
    print("-" * 30)
    print(f"🔴 方案A (普通最短路) 遇到高风险障碍次数: {risk_count_A}")
    print(f"🟢 方案B (安全优先路) 遇到高风险障碍次数: {risk_count_B}")
    print("-" * 30)
    print(f"🛡️  成功规避危险次数: {avoided_danger_count}")
    print(f"📈 成功避险比例: {avoidance_rate:.2f}%")
    print("="*50)
    
    # 5. 保存详细结果
    output_file = "simulation_results.csv"
    df.to_csv(output_file, index=False, encoding='utf-8-sig')
    print(f"\n✅ 详细测试数据已保存至: {output_file}")

if __name__ == "__main__":
    run_simulation()
