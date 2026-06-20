using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using noob.Core.Models.Relics;

namespace noob   // ← 这里改成你的模组ID（必须和你的.json文件中的"id"一致！）
{
    // 通用 ModInitializer
    [ModInitializer(nameof(Initialize))]
    public static class ModInitializer
    {
        public static Logger Logger { get; } = new Logger("noob", (LogType)0);
        
        public static void Initialize()
        {
            {
                // 1. 初始化 Harmony（强烈推荐这样写，避免与其他模组冲突。）
                var harmony = new Harmony("noob.aliang");   // 格式：模组ID.作者名
                harmony.PatchAll();

                // 2. 在这里添加你的注册逻辑（卡牌、遗物、药水等）
                //ModHelper.AddModelToPool(typeof( 卡牌池 ), typeof( 卡牌名字 ));
                ModHelper.AddModelToPool(typeof( SharedRelicPool ), typeof( ALiangNoob ));
                //ModHelper.AddModelToPool(typeof( 药水池 ), typeof( 药水名字 ));
                
                PatchAllCharacterModelSubclasses(harmony);
            }
            Logger.Info("noob加载成功！");// 可以删掉
        }
        
        private static void PatchAllCharacterModelSubclasses(Harmony harmony)
        {
            // 定义要Patch的方法名（属性的getter方法名）
            const string targetMethodName = "get_StartingRelics";
            
            // 获取当前程序域中所有加载的程序集（覆盖STS2所有内置角色类）
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // 筛选条件：
            // - 继承自CharacterModel
            // - 非抽象类（抽象类无法实例化，也无法Patch）
            // - 包含非抽象的get_StartingRelics方法
            var targetTypes = allAssemblies
                .SelectMany(asm =>
                {
                    try { return asm.GetTypes(); }
                    catch (ReflectionTypeLoadException) { return Type.EmptyTypes; } // 跳过加载失败的程序集
                })
                .Where(type => 
                    type != null 
                    && !type.IsAbstract  // 排除抽象类
                    && type.IsSubclassOf(typeof(CharacterModel)) // 继承CharacterModel
                    && GetNonAbstractMethod(type, targetMethodName) != null); // 有非抽象的get_StartingRelics方法

            // 遍历符合条件的子类，逐个打补丁
            foreach (var subclassType in targetTypes)
            {
                try
                {
                    // 获取子类的get_StartingRelics方法
                    var targetMethod = GetNonAbstractMethod(subclassType, targetMethodName);
                    if (targetMethod == null) continue;

                    // 动态Patch该方法（复用统一的Postfix逻辑）
                    harmony.Patch(
                        original: targetMethod,
                        postfix: new HarmonyMethod(typeof(CharacterStartingRelicsPatch), nameof(CharacterStartingRelicsPatch.AddCustomRelicPostfix))
                    );

                    Log.Info($"成功Patch [{subclassType.FullName}] 的 {targetMethodName} 方法");
                }
                catch (Exception ex)
                {
                    Log.Warn($"Patch [{subclassType.FullName}] 失败：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 安全获取类型的非抽象方法（优先取自身实现，无则取父类非抽象实现）
        /// </summary>
        private static MethodInfo GetNonAbstractMethod(Type type, string methodName)
        {
            // 遍历方法（包括继承的，但排除抽象方法）
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null,
                Type.EmptyTypes,
                null
            );

            return method != null && !method.IsAbstract ? method : null;
        }
    }

    public static class CharacterStartingRelicsPatch
    {
        public static Logger Logger { get; } = new Logger("noob", (LogType)0);
        
        public static void AddCustomRelicPostfix(ref IReadOnlyList<RelicModel> __result)
        {
            if (__result == null)
            {
                Logger.Info("Starting relic pool is null!");
                return;
            }
            
            Logger.Info("add a relic into starting relic pool, the pool length is " + __result.Count);
            
            var newRelicList = __result.ToList();
            var customRelic = ModelDb.Relic<ALiangNoob>();
            
            // 3. 避免重复添加（防止补丁多次触发）
            if (!newRelicList.Contains(customRelic))
            {
                newRelicList.Add(customRelic);
            }

            // 4. 转回只读列表，覆盖原返回值
            __result = newRelicList.AsReadOnly();
            Logger.Info("Add completed, the pool length is " + __result.Count);
        }
        
    }
}