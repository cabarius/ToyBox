#if false

 var smallFontSize = 10;
                                    if (profile.Spells.Where(b => b.Name == sp.Name).FirstOrDefault() != null)
                                    {
                                        GUI.backgroundColor = Color.green;
                                        if (sp.Name.Length > 25)
                                        {
                                            GUI.skin.button.fontSize = smallFontSize;
                                            GUI.skin.button.wordWrap = true;
                                        }
                                        if (GUILayout.Button("<b>" + sp.Name + "</b>", GUILayout.Width(220), GUILayout.Height(25)))
                                        {
                                            profile.Spells.Remove(profile.Spells.Where(b => b.Name == sp.Name).FirstOrDefault());
                                        }
                                        GUI.skin.button.fontSize = defaultFontSize;
                                        GUI.skin.button.wordWrap = false;
                                        GUI.backgroundColor = defaultColor;
                                    }


    public class BlueprintLoader : MonoBehaviour {
        AssetBundleRequest loadRequest = null;
        Action<IEnumerable<SimpleBlueprint>> callback;
        public BlueprintLoader(Action<IEnumerable<SimpleBlueprint>> callback) {
            this.callback = callback;
        }
        void Start() {
            StartCoroutine(Load());
        }
        public IEnumerator Load() {
            if (loadRequest == null) {
                var bundle = (AssetBundle)AccessTools.Field(typeof(ResourcesLibrary), "s_BlueprintsBundle").GetValue(null);
                loadRequest = bundle.LoadAllAssetsAsync<SimpleBlueprint>();
            }
            if (!loadRequest.isDone) {
                yield return null;
            }
            else {
                callback(loadRequest.allAssets.Select((a) => (SimpleBlueprint)a));
            }
        }
    }
#endif

/* 
            //if (GL.Button("Add Feature", GL.Width(300f))) {
//    BlueprintActions.addFact(selectedBlueprint);
//}
//if (GL.Button("Remove Feature", GL.Width(300f)))
//{
//    BlueprintActions.removeFact(selectedBlueprint);
//}
//            if (GL.Button("Give Item", GL.Width(300f))) {
//                BlueprintActions.addItem(selectedBlueprint);
////                CheatsUnlock.CreateItem("- " + parameter);
//            }

    //selectedBlueprintIndex = GL.SelectionGrid(selectedBlueprintIndex, filteredBPNames, 4);

    if (selectedBlueprintIndex  >= 0)
    {
        parameter = filteredBPNames[selectedBlueprintIndex];
        selectedBlueprint = filteredBPs[selectedBlueprintIndex];
    }                     blueprints

.Where(bp => bp.name.ToLower().Contains(searchText.ToLower()))
            .OrderBy(bp => bp.name)
            .Take(Settings.searchLimit).ToArray();


you can do it async such as:
var bundle = GetBlueprintAssetBundleFromResourcesLibrary();
var request = AssetBundle.LoadAllAssetsAsync();
request.completed += (asyncOperation) => {
    var blueprints = request.allAssets;
    //process blueprints
};
alternatively, you can use coroutines, such as
const int BatchSize = 1000;
IEnumerable ProcessBlueprints(){
  var guids = GetBlueprintGuids();
  int counter = 0;
  foreach(var guid in guids){
    //yield return to prevent blocking game
    if(counter > BatchSize)
    {
      counter = 0;
      yield return null;
    }
    counter++;
    var blueprint = ResourcesLibrary.TryGetBlueprint<SimpleBlueprint>(guid);
    //Process blueprint        
  }
}
void Start()
{
  StartCoroutine(ProcessBlueprints());
}


GL.Space(10);
            GL.Label("MyFloatOption", GL.ExpandWidth(false));
            GL.Space(10);
            Settings.MyFloatOption = GL.HorizontalSlider(Settings.MyFloatOption, 1f, 10f, GL.Width(300f));
            GL.Label($" {Settings.MyFloatOption:p0}", GL.ExpandWidth(false));
            GL.EndHorizontal();

            GL.BeginHorizontal();
            GL.Label("MyBoolOption", GL.ExpandWidth(false));
            GL.Space(10);
            Settings.MyBoolOption = GL.Toggle(Settings.MyBoolOption, $" {Settings.MyBoolOption}", GL.ExpandWidth(false));
            GL.EndHorizontal();

            GL.BeginHorizontal();
            GL.Label("MyTextOption", GL.ExpandWidth(false));
            GL.Space(10);
            Settings.MyTextOption = GL.TextField(Settings.MyTextOption, GL.Width(300f));
            GL.EndHorizontal()

            */

#if false
                List<SimpleBlueprint> bps = new List<SimpleBlueprint>();
                SimpleBlueprint[] allBPs = GetBlueprints();
                foreach (SimpleBlueprint bp in allBPs)
                {
                    bool ignoreFound = false;
                    foreach (Type t in BlueprintAction.ignoredBluePrintTypes)
                    {
                        if (bp.GetType().IsKindOf(t)) { ignoreFound = true; break; }
                    }
                    if (!ignoreFound)
                    {
                        bps.Add(bp);
                    }
                }
                blueprints = bps.ToArray();
#endif

#if false 
public static BlueprintBuff[] GetAbilityBuffs(BlueprintAbility Ability) {
    return Ability
        .GetComponents<AbilityEffectRunAction>()
        .SelectMany(c => c.Actions.Actions.OfType<ContextActionApplyBuff>()
            .Concat(c.Actions.Actions.OfType<ContextActionConditionalSaved>()
                .SelectMany(a => a.Failed.Actions.OfType<ContextActionApplyBuff>()))
            .Concat(c.Actions.Actions.OfType<Conditional>()
                .SelectMany(a => a.IfTrue.Actions.OfType<ContextActionApplyBuff>()
                    .Concat(a.IfFalse.Actions.OfType<ContextActionApplyBuff>()))))
        .Where(c => c.Buff != null)
        .Select(c => c.Buff)
        .Distinct()
        .ToArray();
}

public static ContextActionApplyBuff[] GetAbilityContextActionApplyBuffs(BlueprintAbility Ability)
        {
            return Ability
                .GetComponents<AbilityEffectRunAction>()
                .SelectMany(c => c.Actions.Actions.OfType<ContextActionApplyBuff>()
                    .Concat(c.Actions.Actions.OfType<ContextActionConditionalSaved>()
                        .SelectMany(a => a.Failed.Actions.OfType<ContextActionApplyBuff>()))
                    .Concat(c.Actions.Actions.OfType<Conditional>()
                        .SelectMany(a => a.IfTrue.Actions.OfType<ContextActionApplyBuff>()
                            .Concat(a.IfFalse.Actions.OfType<ContextActionApplyBuff>()))))
                .Where(c => c.Buff != null).ToArray();
        }

        public static DurationRate[] getAbilityBuffDurations(BlueprintAbility Ability)
        {
            var applyBuffs = GetAbilityContextActionApplyBuffs(Ability);
            return applyBuffs.Select(a => a.UseDurationSeconds ? DurationRate.Rounds : a.DurationValue.Rate).ToArray();
        }



    public IEnumerable<FeatureUIData> GetFullSelectionItems()
    {
      if (this.m_CachedItems == null)
      {
        switch (this.ParameterType)
        {
          case FeatureParameterType.Custom:
          case FeatureParameterType.SpellSpecialization:
          case FeatureParameterType.FeatureSelection:
            this.m_CachedItems = this.ExtractItemsFromBlueprints(((IEnumerable<BlueprintReference<SimpleBlueprint>>) this.BlueprintParameterVariants).Dereference<SimpleBlueprint>()).ToArray<FeatureUIData>();
            break;
          case FeatureParameterType.WeaponCategory:
            this.m_CachedItems = this.ExtractItemsWeaponCategory().ToArray<FeatureUIData>();
            break;
          case FeatureParameterType.SpellSchool:
            this.m_CachedItems = this.ExtractItemsSpellSchool().ToArray<FeatureUIData>();
            break;
          case FeatureParameterType.LearnSpell:
            this.m_CachedItems = this.ExtractItemsFromBlueprints(((IEnumerable<BlueprintReference<SimpleBlueprint>>) this.BlueprintParameterVariants).Dereference<SimpleBlueprint>()).ToArray<FeatureUIData>();
            break;
          case FeatureParameterType.Skill:
            this.m_CachedItems = this.ExtractSkills().ToArray<FeatureUIData>();
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
      return (IEnumerable<FeatureUIData>) this.m_CachedItems;
    }


       public static void LevelUp(AddClassLevels c, UnitDescriptor unit, int levels, UnitFact fact = null)
    {
      using (new IgnorePrerequisites())
      {
        using (ContextData<AddClassLevels.ExecutionMark>.Request())
        {
          int num1;
          if (!c.CharacterClass.IsMythic)
          {
            ClassLevelLimit classLevelLimit = unit.OriginalBlueprint.GetComponent<ClassLevelLimit>().Or<ClassLevelLimit>((ClassLevelLimit) null);
            num1 = classLevelLimit != null ? classLevelLimit.LevelLimit : int.MaxValue;
          }
          else
          {
            MythicLevelLimit mythicLevelLimit = unit.OriginalBlueprint.GetComponent<MythicLevelLimit>().Or<MythicLevelLimit>((MythicLevelLimit) null);
            num1 = mythicLevelLimit != null ? mythicLevelLimit.LevelLimit : int.MaxValue;
          }
          int num2 = num1;
          if (ContextData<DefaultBuildData>.Current != null)
            num2 = 0;
          if (TacticalCombatHelper.IsActive)
          {
            for (int index = 0; index < levels; ++index)
            {
              unit.Progression.AddFakeClassLevel(c.CharacterClass);
              unit.Progression.ReapplyFeaturesOnLevelUp();
            }
          }
          else
          {
            Dictionary<SelectionEntry, HashSet<int>> selectionsHistory = new Dictionary<SelectionEntry, HashSet<int>>();
            HashSet<int> spellHistory = c.SelectSpells.Length > 0 ? new HashSet<int>() : (HashSet<int>) null;
            for (int index = 0; index < levels; ++index)
            {
              if ((c.CharacterClass.IsMythic ? unit.Progression.MythicLevel : unit.Progression.CharacterLevel) < num2)
              {
                using (ProfileScope.New("AddClassLevels.AddLevel"))
                  AddClassLevels.AddLevel(c, unit, selectionsHistory, spellHistory, fact);
              }
              else if (unit.IsPlayerFaction && !(bool) (ContextData<AddClassLevels.DoNotCreatePlan>) ContextData<AddClassLevels.DoNotCreatePlan>.Current)
              {
                UnitDescriptor unit1 = unit.Get<LevelUpPlanUnitHolder>()?.RequestPlan();
                if (unit1 != null)
                {
                  LevelUpController levelUpController = AddClassLevels.AddLevel(c, unit1, selectionsHistory, spellHistory, fact);
                  unit.Progression.AddLevelPlan(levelUpController.GetPlan(), c.CharacterClass.IsMythic);
                }
              }
            }
            unit.Progression.ReapplyFeaturesOnLevelUp();
            AddClassLevels.PrepareSpellbook(c, unit);
            UnitEntityView view = unit.Unit.View;
            if ((UnityEngine.Object) view != (UnityEngine.Object) null)
              view.UpdateClassEquipment();
            RestController.ApplyRest(unit);
          }
        }
      }
    }

      private static void PerformSelections(
      AddClassLevels c,
      LevelUpController controller,
      Dictionary<SelectionEntry, HashSet<int>> selectionsHistory,
      LevelUpActionPriority? maxPriority = null)
    {
      SelectionEntry[] selections = c.Selections;
label_24:
      for (int index = 0; index < selections.Length; ++index)
      {
        SelectionEntry key = selections[index];
        if (maxPriority.HasValue)
        {
          if (key.IsParametrizedFeature)
          {
            if (SelectFeature.CalculatePriority((IFeatureSelection) key.ParametrizedFeature) > maxPriority.Value)
              continue;
          }
          else if (SelectFeature.CalculatePriority((IFeatureSelection) key.Selection) > maxPriority.Value)
            continue;
        }
        HashSet<int> intSet;
        if (!selectionsHistory.TryGetValue(key, out intSet))
        {
          intSet = new HashSet<int>();
          selectionsHistory[key] = intSet;
        }
        if (key.IsParametrizedFeature)
        {
          FeatureSelectionState selection = controller.State.FindSelection((IFeatureSelection) key.ParametrizedFeature);
          if (selection != (FeatureSelectionState) null)
          {
            FeatureUIData featureUiData;
            switch (key.ParametrizedFeature.ParameterType)
            {
              case FeatureParameterType.Custom:
              case FeatureParameterType.SpellSpecialization:
              case FeatureParameterType.FeatureSelection:
                featureUiData = new FeatureUIData((BlueprintFeature) key.ParametrizedFeature, (FeatureParam) key.ParamObject, "", "", (Sprite) null, key.ParamObject.ToString());
                break;
              case FeatureParameterType.WeaponCategory:
                featureUiData = new FeatureUIData((BlueprintFeature) key.ParametrizedFeature, (FeatureParam) key.ParamWeaponCategory, "", "", (Sprite) null, key.ParamWeaponCategory.ToString());
                break;
              case FeatureParameterType.SpellSchool:
                featureUiData = new FeatureUIData((BlueprintFeature) key.ParametrizedFeature, (FeatureParam) key.ParamSpellSchool, "", "", (Sprite) null, key.ParamSpellSchool.ToString());
                break;
              case FeatureParameterType.Skill:
                featureUiData = new FeatureUIData((BlueprintFeature) key.ParametrizedFeature, (FeatureParam) key.Stat, "", "", (Sprite) null, key.Stat.ToString());
                break;
              default:
                throw new ArgumentOutOfRangeException();
            }
            controller.SelectFeature(selection, (IFeatureSelectionItem) featureUiData);
          }
        }
        else
        {
          int i = 0;
          while (true)
          {
            int num = i;
            ReferenceArrayProxy<BlueprintFeature, BlueprintFeatureReference> features = key.Features;
            int length = features.Length;
            if (num < length)
            {
              if (!intSet.Contains(i))
              {
                features = key.Features;
                BlueprintFeature blueprintFeature = features[i];
                FeatureSelectionState selection = controller.State.FindSelection((IFeatureSelection) key.Selection);
                if (selection != (FeatureSelectionState) null && (UnityEngine.Object) blueprintFeature != (UnityEngine.Object) null && controller.SelectFeature(selection, (IFeatureSelectionItem) blueprintFeature))
                  intSet.Add(i);
              }
              ++i;
            }
            else
              goto label_24;
          }
        }
      }
    }


      public void CopyFrom(UnitProgressionData other)
    {
      this.SetRace(other.Race);
      this.Experience = other.Experience;
      this.MythicExperience = other.MythicExperience;
      foreach (ClassData classData1 in other.Classes)
      {
        for (int index = 1; index <= classData1.Level; ++index)
        {
          if (this.GetClassLevel(classData1.CharacterClass) < index)
            this.AddClassLevel(classData1.CharacterClass);
          if (index == 1)
          {
            foreach (BlueprintArchetype archetype in classData1.Archetypes)
              this.AddArchetype(classData1.CharacterClass, archetype);
          }
        }
        ClassData classData2 = this.GetClassData(classData1.CharacterClass);
        if (classData2 != null)
        {
          classData2.Spellbook = classData1.Spellbook;
          classData2.PriorityEquipment = classData1.PriorityEquipment;
        }
      }
      foreach (KeyValuePair<BlueprintProgression, ProgressionData> progression in other.m_Progressions)
        this.SureProgressionData(progression.Key).Level = progression.Value.Level;
      foreach (KeyValuePair<BlueprintFeatureSelection, FeatureSelectionData> selection1 in other.m_Selections)
      {
        BlueprintFeatureSelection key1;
        FeatureSelectionData featureSelectionData1;
        selection1.Deconstruct<BlueprintFeatureSelection, FeatureSelectionData>(out key1, out featureSelectionData1);
        BlueprintFeatureSelection selection2 = key1;
        FeatureSelectionData featureSelectionData2 = featureSelectionData1;
        foreach (KeyValuePair<int, List<BlueprintFeature>> tuple in featureSelectionData2.SelectionsByLevel)
        {
          int key2;
          List<BlueprintFeature> blueprintFeatureList1;
          tuple.Deconstruct<int, List<BlueprintFeature>>(out key2, out blueprintFeatureList1);
          int level = key2;
          List<BlueprintFeature> blueprintFeatureList2 = blueprintFeatureList1;
          List<BlueprintFeature> selections = this.GetSelections(selection2, level);
          foreach (BlueprintFeature feature in blueprintFeatureList2)
          {
            if (!selections.HasItem<BlueprintFeature>(feature))
              this.AddSelection(selection2, featureSelectionData2.Source, level, feature);
          }
        }
      }
      this.m_LevelPlans.Clear();
      this.m_LevelPlans.AddRange((IEnumerable<LevelPlanData>) other.m_LevelPlans);
      this.m_MythicLevelPlans.Clear();
      this.m_MythicLevelPlans.AddRange((IEnumerable<LevelPlanData>) other.m_MythicLevelPlans);
    }

#endif

