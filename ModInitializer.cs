using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
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
                
                //PatchAllCharacterModelSubclasses(harmony);
            }
            Logger.Info("noob加载成功！");// 可以删掉
        }
        
    }
    
    [HarmonyPatch(typeof(Player), "PopulateStartingRelics", MethodType.Normal)]
    public static class PlayerPopulateRelicsPatch
    {
        private static MethodInfo _addRelicInternalMethod;

        static PlayerPopulateRelicsPatch()
        {
            _addRelicInternalMethod = AccessTools.Method(
                typeof(Player),
                "AddRelicInternal",
                new[] { typeof(RelicModel), typeof(int), typeof(bool) }
            );
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            // 关键修复：加上 .ToMutable() 生成可变副本
            RelicModel canonicalRelic = ModelDb.Relic<ALiangNoob>();
            RelicModel customRelic = canonicalRelic.ToMutable();

            if (customRelic == null) return;

            // 按ID查重
            bool alreadyHas = __instance.Relics.Any(r => r.Id == customRelic.Id);
            if (alreadyHas) return;

            // 调用内部方法添加
            _addRelicInternalMethod.Invoke(__instance, new object[] { customRelic, -1, false });
        }
    }
}