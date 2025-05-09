namespace ChaosServer;

public class Exam
{
    public class MaterialData
    {
        public ItemData item;   //合成所需的物品
        public int count;       //合成所需的该物品的数量
    }

    public class ItemData
    {
        public int id;                          //物品 ID
        public int count;                       //当前拥有的物品数量
        public int costGold;                    //合成该物品所需的金币
        public List<MaterialData> materialList; //合成该物品所需的材料
    }

    /// <summary>
    /// 计算用 totalGold 金币最多可以合成的 item 装备的数量
    /// </summary>
    /// <param name="item">要合成的装备</param>
    /// <param name="totalGold">拥有的金币</param>
    /// <returns>可合成的 item 装备的最大数量</returns>
    public int Run(ItemData item, int totalGold)
    {
        // int count = Calc(item, ref totalGold, 0);
        // return count;
        ItemCombineResult itemCombineResult = Calc(item, totalGold, true);
        return itemCombineResult.count;
    }
    
    /// <summary>
    /// 每层合成装备后的返回结果
    /// </summary>
    public struct ItemCombineResult
    {
        // 最多合成多少物品
        public int count;
        // 单个花费
        public int perCost;
    }

    /// <summary>
    /// 计算每层最多可以合成的物品数量和单价
    /// </summary>
    /// <param name="item">需要合成的物品</param>
    /// <param name="totalGold">拥有的金币</param>
    /// <param name="isHead">是否为第一个遍历的节点</param>
    /// <returns>每层最多可以合成的物品数量和单价</returns>
    private ItemCombineResult Calc(ItemData item, int totalGold, bool isHead)
    {
        // E => 2  F => 9   C => 2  G => 10 totalGold = 120
        // if (matList == null) return 0;
        if (item.materialList == null)
        {
            // 叶子节点
            // 返回当前叶子节点的物品数量
            return new ItemCombineResult
            {
                count = item.count,
            };
        }

        // 获取合成该物品所需的材料List
        List<MaterialData> itemMaterialList = item.materialList;
        int itemPerCost = 0;
        List<ItemCombineResult> itemCombineResultList = new List<ItemCombineResult>();
        for (int i = 0; i < itemMaterialList.Count; i++)
        {
            ItemCombineResult result = Calc(itemMaterialList[i].item, totalGold, false);
            itemPerCost += result.perCost;
            itemCombineResultList.Add(result);
        }

        // 获取最多能合成多少个物品（单纯从数量计算）
        int max = int.MaxValue;
        for (int i = 0; i < itemCombineResultList.Count; i++)
        {
            max = Math.Min(max, itemCombineResultList[i].count / itemMaterialList[i].count);
        }

        if (isHead)
        {
            while (true)
            {
                // 计算顶部物品合成的总价
                int totalCost = 0;
                for (int i = 0; i < itemCombineResultList.Count; i++)
                {
                    totalCost += itemCombineResultList[i].perCost * itemMaterialList[i].count * max;
                }
                // 如果超出 totalGold, 最多可以合成的物品自减
                if (totalCost + item.costGold * max > totalGold)
                {
                    max--;
                }
                else
                {
                    break;
                }
            }
        }
        
        return new ItemCombineResult
        {
            count = max,
            perCost = itemPerCost + item.costGold
        };
    }

    private static ItemData LoadTestData()
    {
        MaterialData matE = new MaterialData
        {
            item = new ItemData
            {
                id = 1,
                costGold = 0,
                count = 99,
            },
            count = 1,
        };
        MaterialData matF = new MaterialData
        {
            item = new ItemData
            {
                id = 2,
                costGold = 0,
                count = 540,
            },
            count = 5,
        };
        
        MaterialData matG = new MaterialData
        {
            item = new ItemData
            {
                id = 4,
                costGold = 0,
                count = 108,
            },
            count = 9,
        };
        List<MaterialData> matListB = new List<MaterialData> { matE, matF };
        List<MaterialData> matListD = new List<MaterialData> { matG };

        ItemData itemB = new ItemData
        {
            id = 5,
            costGold = 53,
            materialList = matListB
        };
        // ItemData itemC = new ItemData
        // {
        //     id = 3,
        //     costGold = 0,
        //     count = 2,
        // };
        ItemData itemD = new ItemData
        {
            id = 6,
            costGold = 58,
            materialList = matListD
        };
        MaterialData matC = new MaterialData
        {
            item = new ItemData
            {
                id = 3,
                count = 3,
            },
            count = 1,
        };

        MaterialData matB = new MaterialData
        {
            count = 3,
            item = itemB
        };
        MaterialData matD = new MaterialData
        {
            count = 4,
            item = itemD
        };

        List<MaterialData> matListA = new List<MaterialData> { matB, matC, matD };
        ItemData itemA = new ItemData
        {
            id = 7,
            costGold = 26,
            materialList = matListA
        };
        return itemA;
    }
}
