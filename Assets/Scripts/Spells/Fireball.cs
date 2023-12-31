﻿using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[Serializable()]
public class Fireball : Battle_Entity_Spells {

    public Fireball() : 
        base(20,
            10,
            1,
            "The user calls upon the power of fire to create a concentrated ball of fire to throw towards their enemies.",
            "Fireball",
            '火',
            Battle_Entity.Faction.Enemy,
            "Audio/SFX/Fireball",
            "Animations/Spells/Fireball") {}

    public override void CastSpell(List<Battle_Entity> targets, Battle_Entity caster) {
        if (caster.GetStats().currMana < manaCost) {
            return;
        }

        caster.ReduceMana(manaCost);
        targets[0].TakeDamage(caster.GetStats().mag + baseMag, DamageType.Magical);
        PlaySound(caster.GetSFXManager());
        PlayAnimation(targets, new List<Battle_Entity>() { caster });
    }

    public override void PlayAnimation(List<Battle_Entity> targets, List<Battle_Entity> sources) {
        GameObject spellPrefab = GameObject.Instantiate(Resources.Load("Prefabs/Spell") as GameObject);
        Animator animator = null;
        Spell_Animation_Behaviour animBehaviour = null;
        if (spellPrefab != null) {
            animator = spellPrefab.GetComponent<Animator>();
            animBehaviour = spellPrefab.GetComponent<Spell_Animation_Behaviour>();
        }

        spellPrefab.transform.position = targets[0].transform.position + new Vector3(-2.5f, 2.5f);
        animBehaviour.SetMoveSpeed(0.03f);
        animBehaviour.SetTargetTransform(targets[0].transform);

        animator.runtimeAnimatorController = Resources.Load(animatorController) as RuntimeAnimatorController;
        animator.enabled = true;
    }
}
