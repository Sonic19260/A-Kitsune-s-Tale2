﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Battle_Entity : MonoBehaviour {
    public enum Faction {
        Ally,
        Enemy,
        Ally_Spirit,
        Enemy_Spirit,
        Neutral,
        NULL
    };

    private string unitName = "\n";
    private Faction unitFaction = Faction.NULL;
    private Battle_Entity_Stats battleStats;
    private List<Battle_Entity_Stat_Change> statChanges;
    private bool isGuarding;
    private Battle_Entity_Loadout loadout;
    private List<Battle_Entity_Spells> spells;
    private List<Item> items;

    [SerializeField]
    private GameObject barPrefab;

    [SerializeField]
    private List<Sprite> platformSprites;

    private Battle_Entity_Bar hpBar;
    private Battle_Entity_Bar manaBar;

    private SpriteRenderer spriteRend;
    private SpriteRenderer shieldRenderer;
    private SpriteRenderer platformRenderer;
    private bool fadeIn;
    private float fadeSpeed;

    private Animator animator, effectAnimator;

    private Sound_Manager sfxManager;

    private bool isDying;
    private bool finishedDying;
    private float dieSpeed;

    private void Awake() {
        hpBar = Instantiate(barPrefab, this.transform).GetComponent<Battle_Entity_Bar>();
        hpBar.SetColor(new Color(0, 255, 0));
        hpBar.transform.localPosition = new Vector3(0f, -1.5f, 0f);

        manaBar = Instantiate(barPrefab, this.transform).GetComponent<Battle_Entity_Bar>();
        manaBar.SetColor(new Color(0, 0, 255));
        manaBar.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        statChanges = new List<Battle_Entity_Stat_Change>();
        isGuarding = false;

        loadout = new Battle_Entity_Loadout();

        spells = new List<Battle_Entity_Spells>();

        items = new List<Item>();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        spriteRend = renderers[0];

        for (int i = 0; i < renderers.Length; i++) {
            if (renderers[i].gameObject.name == "Shield") {
                shieldRenderer = renderers[i];
            } else if (renderers[i].gameObject.name == "Unit Platform") {
                platformRenderer = renderers[i];

                platformRenderer.sprite = platformSprites[Utils.backgroundUsed - 1];
            }
        }
        fadeIn = true;
        fadeSpeed = 0.006f;

        Animator[] animators = GetComponentsInChildren<Animator>();
        animator = animators[0];

        effectAnimator = animators[1];

        isDying = false;
        finishedDying = false;
        dieSpeed = 0.0005f;
    }

    private void Start() {
        sfxManager = GameObject.Find("SFX Handler").GetComponent<Sound_Manager>();
    }

    void Update() {
        if (isDying) {
            if (!finishedDying) {
                DoDeathAnimation();
            }
            return;
        }
        
        if (isGuarding) {
            RenderShield();
        }
    }

    public void BasicAttack(List<Battle_Entity> targets) {
        foreach (Weapon weapon in loadout.GetWeapons()) {
            if (weapon is No_Weapon) {
                continue;
            }

            float premitigationDamage;
            DamageType damageType = weapon.GetDamageType();

            switch(damageType) {
                case DamageType.Physical: {
                    premitigationDamage = battleStats.str;
                    break;
                }
                case DamageType.Magical: {
                    premitigationDamage = battleStats.mag;
                    break;
                }
                case DamageType.NULL: 
                default: {
                    premitigationDamage = 0f;
                    break;
                }
            }

            animator.SetTrigger("isAttacking");
            sfxManager.PlaySound("Physical Attack");
            foreach (Battle_Entity target in targets) {
                target.TakeDamage(premitigationDamage, damageType);
                Animator targetAnimator = target.GetEffectAnimator();
                targetAnimator.runtimeAnimatorController = Resources.Load("Animations/Effects/Slash") as RuntimeAnimatorController;
                targetAnimator.enabled = true;
            }
        }
    }

    public void RaiseGuard() {
        isGuarding = true;
    }

    public void LowerGuard() {
        isGuarding = false;
        shieldRenderer.color = new Color(1, 1, 1, 0);
    }

    public void Heal(float amount) {
        if (battleStats.currHP > 0.0f) {
            battleStats.currHP = Mathf.Min(battleStats.currHP + amount, battleStats.maxHP);

            hpBar.SetTargetPercentage(battleStats.currHP / battleStats.maxHP);
            hpBar.gameObject.SetActive(true);
        }
    }

    public void TakeDamage(float damage, DamageType damageType) {
        float damageReduction;
        switch (damageType) {
            case DamageType.Physical: {
                damageReduction = battleStats.def;
                break;
            }
            case DamageType.Magical: {
                damageReduction = battleStats.res;
                break;
            }
            case DamageType.NULL:
            default: {
                damageReduction = 0f;
                break;
            }
        }

        float damageDealt = damage - damageReduction;
        if (damageDealt < 0f) {
            return;
        }

        if (isGuarding) {
            damageDealt /= 2f;
            LowerGuard();
            sfxManager.PlaySound("Block");
        }

        if (damageDealt < battleStats.currHP) {
            battleStats.currHP -= damageDealt;
        } else {
            battleStats.currHP = 0;
            StartDying();
        }

        hpBar.SetTargetPercentage(battleStats.currHP / battleStats.maxHP);
        hpBar.gameObject.SetActive(true);

        animator.SetTrigger("isHurt");
    }

    private void StartDying() {
        isDying = true;
    }

    private void DoDeathAnimation() {
        animator.SetTrigger("isHurt");

        Color spriteColor = spriteRend.color;
        spriteColor.a -= dieSpeed;

        if (spriteColor.a <= 0f) {
            finishedDying = true;
        }

        spriteRend.color = spriteColor;
    }

    public void RestoreMana(float amount) {
        battleStats.currMana = Mathf.Min(battleStats.currMana + amount, battleStats.maxMana);

        manaBar.SetTargetPercentage(battleStats.currMana / battleStats.maxMana);
        manaBar.gameObject.SetActive(true);
    }

    public void ReduceMana(float amount) {
        battleStats.currMana = Mathf.Max(battleStats.currMana - amount, 0);

        manaBar.SetTargetPercentage(battleStats.currMana / battleStats.maxMana);
        manaBar.gameObject.SetActive(true);
    }

    public void AddStatChange(Battle_Entity_Stat_Change newStatChange) {
        statChanges.Add(newStatChange);
        statChanges[statChanges.Count - 1].ApplyStatChanges();
    }

    public void CheckStatChanges() {
        foreach (Battle_Entity_Stat_Change statChange in statChanges) {
            statChange.LowerTurnCount();
        }

        statChanges.RemoveAll(statChange => statChange.GetReadyToRemove() == true);
    }

    public void GrantExperience(float amount) {
        if (battleStats.currXP + amount >= battleStats.maxXP) {
            battleStats.currXP = amount - (battleStats.maxXP - battleStats.currXP);
            LevelUp();
        } else {
            battleStats.currXP += amount;
        }
    }

    private void LevelUp() {
        battleStats.maxXP *= 1.5f;
        battleStats.level++;

        battleStats.maxHP = battleStats.currHP = battleStats.maxHP + 100;
        battleStats.maxMana = battleStats.currMana = battleStats.maxMana + 100;
        battleStats.str += 5;
        battleStats.mag += 5;
        battleStats.spd += 5;
        battleStats.def += 5;
        battleStats.res += 5;
    }

    private void RenderShield() {
        Color shieldColor = shieldRenderer.color;
        if (fadeIn) {
            shieldColor.a += fadeSpeed;

            if (shieldColor.a >= 1f) {
                fadeIn = false;
            }
        } else {
            shieldColor.a -= fadeSpeed;

            if (shieldColor.a <= 0f) { 
                fadeIn = true;
            }
        }

        shieldRenderer.color = shieldColor;
    }

    public Battle_Entity_Stats GetStats() {
        return battleStats;
    }

    public Battle_Entity_Loadout GetLoadout() {
        return loadout;
    }

    public List<Battle_Entity_Spells> GetSpells() {
        return spells;
    }

    public List<Item> GetItems() {
        return items;
    }

    public string GetName() {
        return unitName;
    }

    public Faction GetFaction() {
        return unitFaction;
    }

    public void SetStats(Battle_Entity_Stats newStats) {
        battleStats = new Battle_Entity_Stats(newStats);
    }

    public void SetLoadout(Battle_Entity_Loadout newLoadout) {
        loadout = new Battle_Entity_Loadout(newLoadout);
    }

    public void SetSpells(List<Battle_Entity_Spells> newSpells) {
        spells = new List<Battle_Entity_Spells>(newSpells);
    }

    public void SetItems(List<Item> newItems) {
        items = new List<Item>(newItems);
    }

    public void SetName(string newName) {
        if (unitName == "\n") {
            unitName = newName;
            name = newName;
        }
    }

    public void LoadSprites() {
        if (unitName == Utils.username) {
            animator.runtimeAnimatorController = Resources.Load("Animations/Player/Player_Combat") as RuntimeAnimatorController;

            platformRenderer.gameObject.transform.localPosition = new Vector3(0f, -0.3f, 0f);
        } else {
            animator.runtimeAnimatorController = Resources.Load("Animations/" + unitName + "/" + unitName) as RuntimeAnimatorController;

            Transform[] transforms = GetComponentsInChildren<Transform>();

            for (int i = 0; i < transforms.Length; i++) {
                transforms[i].localScale = new Vector3(transforms[i].localScale.x * -1f, transforms[i].localScale.y, transforms[i].localScale.z);
            }

            platformRenderer.gameObject.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        }
    }

    public void SetFaction(Faction newFaction) {
        unitFaction = newFaction;
    }

    public Sound_Manager GetSFXManager() {
        return sfxManager;
    }

    public Animator GetEffectAnimator() {
        return effectAnimator;
    }

    public bool GetIsDying() {
        return isDying;
    }

    public bool GetFinishedDying() {
        return finishedDying;
    }
}
