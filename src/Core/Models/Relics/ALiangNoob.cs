using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace noob.Core.Models.Relics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;


// 类名改为Noob，保持继承RelicModel
public sealed class ALiangNoob : RelicModel
{
    // 1. 替换原_wasUsed：用使用次数替代布尔值，初始0次，最大3次
    private int _useCount;
    private const int MaxUseTimes = 3; // 固定最大使用次数为3

    // 2. 稀有度可保持Rare，也可根据需求改（比如Common/Uncommon）
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 3. 调整IsUsedUp逻辑：使用次数≥3时视为“耗尽”
    public override bool IsUsedUp => _useCount >= MaxUseTimes;

    public override bool ShowCounter => true;

    public override int DisplayAmount => MaxUseTimes - ALiangNoobUseCount;

    // 4. 回血比例改为100%（替换原50m）
    protected override IEnumerable<DynamicVar> CanonicalVars {
        get { return new[]
        {
            new HealVar(100m), 
            new DynamicVar("NoDeathTimes", 3m),
            new EnergyVar(0),
            new DynamicVar("Power", 0m)
        }.AsReadOnly(); }
    }

    // 5. 保存使用次数（替换原WasUsed），加[SavedProperty]确保存档保留
    [SavedProperty]
    public int ALiangNoobUseCount
    {
        get
        {
            return _useCount;
        }
        set
        {
            AssertMutable(); // 防止非法修改
            _useCount = value;
        }
    }

    // 6. 复活时机判定：仅所有者+未用完次数时阻止死亡
    public override bool ShouldDieLate(Creature creature)
    {
        // 非所有者生物，正常死亡
        if (creature != base.Owner.Creature)
        {
            return true;
        }
        // 次数用完，正常死亡
        if (IsUsedUp)
        {
            return true;
        }
        // 满足条件，阻止死亡（触发复活）
        
        return false;
    }

    // 7. 复活后逻辑：次数+1 + 100%回血
    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash(); // 保留视觉特效
        ALiangNoobUseCount += 1; // 每次复活次数+1（替代原WasUsed=true）
        InvokeDisplayAmountChanged();
        
        DynamicVars["NoDeathTimes"].BaseValue = MaxUseTimes - ALiangNoobUseCount;
        DynamicVars["Power"].BaseValue = CalculatePower();
        base.DynamicVars.Energy.BaseValue = CalculatePower();
        
        // 计算100%最大生命值，至少回1点（避免极端情况）
        decimal amount = Math.Max(1m, (decimal)creature.MaxHp * (base.DynamicVars.Heal.BaseValue / 100m));
        // 执行回血
        await CreatureCmd.Heal(creature, amount);
        
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == base.Owner && ALiangNoobUseCount >= 1)
        {
            Flash();
            var power = CalculatePower();
            await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, power, base.Owner.Creature, null);
            await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, power, base.Owner.Creature, null);
            
        }
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props,
        Creature dealer, CardModel cardSource)
    {
        if (ALiangNoobUseCount >= 1)
        {
            if (target == base.Owner.Creature && dealer != null && dealer.IsEnemy && props.IsPoweredAttack())
            {
                Flash();
                await CreatureCmd.GainMaxHp(target, 1m);
            }
        }
    }
    
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player == base.Owner && ALiangNoobUseCount >= 1)
        {
            var power = CalculatePower();
            return amount + (decimal)power;
        }
        
        return amount;
    }

    private decimal CalculatePower()
    {
        var power = 1;
        for (var i = 0; i < ALiangNoobUseCount; i++)
        {
            power *= ALiangNoobUseCount;
        }
        return power;
    }
}