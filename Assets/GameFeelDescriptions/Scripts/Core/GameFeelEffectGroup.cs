﻿using System;
using System.Collections.Generic;

using System.Linq;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

using UnityEngine;
using Object = UnityEngine.Object;

namespace GameFeelDescriptions
{
    [Serializable]
    public class GameFeelEffectGroup
    {
        /// <summary>
        /// Name of the group, useful for telegraphing what this group is meant to do.
        /// </summary>
        public string GroupName;
        
        /// <summary>
        /// Whether the whole group of effects is disabled.
        /// </summary>
        [Header("Controls if the effects are executed, and adhere to current timeScale.")]
        public bool Disabled;

        /// <summary>
        /// Use unscaledTime when executing this effect, this allows effects to be follow game play time scale or not. 
        /// </summary>
        public bool UnscaledTime;
        
        [Header("This mode allows you to add effects as events happen while playing.")]
        public bool StepThroughMode;
        
        /// <summary>
        /// What is the target this effect is applied to. 
        /// </summary>
        [EnableFieldIf("TargetTag", GameFeelTarget.Tag)]
        [EnableFieldIf("TargetComponentType", GameFeelTarget.ComponentType)] 
        [EnableFieldIf("TargetList", GameFeelTarget.List)]
        [Header("Controls which targets the effects apply to.")]
        [ShowTypeAttribute]
        public GameFeelTarget AppliesTo;

        //TODO: don't show the targetTag, component or list, unless selected in AppliesTo 11/02/2020
        /// <summary>
        /// Used when <see cref="GameFeelEffect.AppliesTo"/> is set to <see cref="GameFeelTarget.Tag"/>.
        /// Applies to objects with the given Tag, or Name.
        /// </summary>
        [HideInInspector] //Visibility handled by AppliesTo Attributes.
        public string TargetTag;

        //TODO: Make this assignable without an actual instance or remove it. 04/02/2020
        /// <summary>
        /// Used when <see cref="GameFeelEffect.AppliesTo"/> is set to <see cref="GameFeelTarget.ComponentType"/>.
        /// Applies to all game objects with a Component of the type attached.
        /// </summary>
        [HideInInspector] //Visibility handled by AppliesTo Attributes.
        public Component TargetComponentType;

        /// <summary>
        /// Used when <see cref="GameFeelEffect.AppliesTo"/> is set to <see cref="GameFeelTarget.List"/>.
        /// Applies to all game objects in the list.
        /// </summary>
        //[HideInInspector] //Visibility handled by AppliesTo Attributes.
        //[HideFieldIf("AppliesTo", GameFeelTarget.List, true)]
        [HideInInspector]
        public List<GameObject> TargetList;

        /// <summary>
        /// Whether to use a copy for executing the effects (to avoid altering game logic)
        /// </summary>
        [HideFieldIf("AppliesTo", GameFeelTarget.EditorValue)]
        [EnableFieldIf("DisableRendererAndFollowOriginal", true)]
        [Tooltip("Controls whether targets get copied and applied to the copies instead.")]
        public bool ExecuteOnTargetCopy;
//TODO: figure out if this should be a maintained visual copy ex. have a ref to original, and make sure it lerps to original transform, whenever some property is not "tweening".
//TODO: And once the original game object is destroyed, the maintained object should be removed once no "effects" are operating on it. 11/4/2020

        /// <summary>
        /// Disable the original renderer, and set the copy to follow the transform.
        /// </summary>
        [HideInInspector]
        [EnableFieldIf("FollowEasing", true)]
        public bool DisableRendererAndFollowOriginal;

        [HideInInspector]
        public EasingHelper.EaseType FollowEasing;

        /// <summary>
        /// The list of effects to execute
        /// </summary>
        [SerializeReference] 
        [ShowTypeAttribute]
        [Space]
        public List<GameFeelEffect> EffectsToExecute = new List<GameFeelEffect>();

        #region Find Targets

        //TODO: Should only be executed on Start, GameObject creation and should remove destroyed elements. 04/02/2020
        /// <summary>
        /// Find targets to execute the effect on.
        /// </summary>
        /// <returns></returns>
        public List<GameObject> FindTargets()
        {
            var targets = new List<GameObject>();

            switch (AppliesTo)
            {
                case GameFeelTarget.Self:
                case GameFeelTarget.Other:
                case GameFeelTarget.EditorValue:
                    //No target pre-fetching needed with Relative targets.
                    break;
                case GameFeelTarget.Tag:
                    if (string.IsNullOrEmpty(TargetTag))
                    {
                        Debug.LogException(new Exception("Need specify a TargetTag, when using GameFeelTarget.Named"));
                    }
                    else
                    {
                        targets.AddRange(GameObject.FindGameObjectsWithTag(TargetTag));
                    }

                    break;
                case GameFeelTarget.ComponentType:
                    if (TargetComponentType == null)
                    {
                        Debug.LogException(
                            new Exception(
                                "Need specify a TargetComponentType, when using GameFeelTarget.ComponentType"));
                    }
                    else
                    {
                        var objects = Object.FindObjectsOfType(TargetComponentType.GetType());
                        targets.AddRange(objects.Select(item => ((Component) item).gameObject));
                    }

                    break;
                case GameFeelTarget.List:
                    //TODO: we can probably remove this null check because it's a MonoBehaviour. 04/02/2020
                    if (TargetList == null || TargetList.Count == 0)
                    {
                        Debug.LogException(
                            new Exception("Need to add objects to TargetList, when using GameFeelTarget.List"));
                    }
                    else
                    {
                        targets.AddRange(TargetList);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return targets;
        }

        public GameObject GetActualTarget(GameObject target, bool dontDestroyImmediate = false)
        {
            if (ExecuteOnTargetCopy)
            {
                //Get a copy and remove all scripts, rigidbodies and colliders.
                var copy = Object.Instantiate(target, GameFeelEffectExecutor.Instance.transform, true);
                copy.tag = "Untagged";
                
                //If the object was deactivated, activate our copy.
                copy.SetActive(true);

                var scripts = copy.GetComponentsInChildren<MonoBehaviour>();
                foreach (var script in scripts)
                {
                    if (dontDestroyImmediate)
                    {
                        Object.Destroy(script);
                    }
                    else
                    {
                        Object.DestroyImmediate(script);
                    }
                }

                var rigid = copy.GetComponent<Rigidbody>();
                if (dontDestroyImmediate)
                {
                    Object.Destroy(rigid);
                }
                else
                {
                    Object.DestroyImmediate(rigid);
                }

                var rigid2D = copy.GetComponent<Rigidbody2D>();
                if (dontDestroyImmediate)
                {
                    Object.Destroy(rigid2D);
                }
                else
                {
                    Object.DestroyImmediate(rigid2D);
                }

                var col = copy.GetComponent<Collider>();
                if (dontDestroyImmediate)
                {
                    Object.Destroy(col);
                }
                else
                {
                    Object.DestroyImmediate(col);
                }

                var col2D = copy.GetComponent<Collider2D>();
                if (dontDestroyImmediate)
                {
                    Object.Destroy(col2D);
                }
                else
                {
                    Object.DestroyImmediate(col2D);
                }

                return copy;
            }

            return target;
        }

        #endregion

        //TODO: consider changing "Vector3? direction" to "TriggerType type, object context", and letting effects cast to respond to different types of context. 7/4/2020
        public void InitializeAndQueueEffects(GameObject origin, List<GameObject> targets, Vector3? direction = null, bool dontDestroyImmediate = false)
        {
            //If the effect group is disabled, do nothing.
            if (Disabled) return;

            if (AppliesTo.ShouldUpdateAtRuntime())
            {
                //TODO: Check that it makes sense to call FindTargets for every call 01/04/2020
                //TODO: should targets be updated per call or not? Maybe use a lastDirty timeStamp per tag.
                targets = FindTargets();
            }

            //TODO: Override currently running effect on origin + target (unless target is singleton, such as Time.timeScale, then override always?) 20/02/2020

            var actualTargets = new GameObject[targets.Count];
            for (int i = 0; i < actualTargets.Length; i++)
            {
                actualTargets[i] = GetActualTarget(targets[i], dontDestroyImmediate);
            }

            //Setup effects on each of the targets.
            for (int outer = 0; outer < EffectsToExecute.Count; outer++)
            {
                //If the effect is disabled, skip it.
                if (EffectsToExecute[outer].Disabled) continue;

                if (AppliesTo == GameFeelTarget.EditorValue)
                {
                    var copy = EffectsToExecute[outer].CopyAndSetElapsed(origin, null, UnscaledTime, direction);

                    if (copy == null) continue;

                    //Find previously active copy
                    var previous = copy.CurrentActiveEffect();

                    //Handle overlapping
                    var (queueCopy, _) = copy.HandleEffectOverlapping(previous);

                    //Queue the effect
                    if (queueCopy)
                    {
                        copy.QueueExecution();   
                    }
                }
                else
                {
                    if (actualTargets.Length == 0)
                    {
                        Debug.Log(EffectsToExecute[outer].GetType().Name +
                                  ": No targets defined at execution time. \nSet AppliesTo to None, if no target is needed for the effect.");
                        return;
                    }

                    if (AppliesTo.IsRelative() && actualTargets.Length > 1)
                    {
                        Debug.LogError(EffectsToExecute[outer].GetType().Name +
                                       ": Too many targets at execution time.");
                    }

                    //Copy for each target and setup each effect, then Queue them in the executor.
                    for (var inner = 0; inner < actualTargets.Length; inner++)
                    {
                        var copy = EffectsToExecute[outer].CopyAndSetElapsed(origin, actualTargets[inner], UnscaledTime, direction);

                        if (copy == null) break;

                        //Find previously active copy
                        var previous = copy.CurrentActiveEffect();

                        //Handle overlapping
                        var (queueCopy, _) = copy.HandleEffectOverlapping(previous);

                        //Queue the effect
                        if (queueCopy)
                        {
                            copy.QueueExecution();   
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Load effects from a recipe into this Description
        /// </summary>
        /// <param name="recipe"></param>
        public void AddEffectsFromRecipe(GameFeelRecipe recipe)
        {
            //If name is empty also add name
            if (string.IsNullOrEmpty(GroupName))
            {
                GroupName = recipe.Name;
            }
            
            EffectsToExecute.AddRange(recipe.effects);
        }
         
        
        /// <summary>
        /// Load effects from a recipe into this Description
        /// </summary>
        /// <param name="recipe"></param>
        public void AddEffectsFromRecipe(GameFeelEffectGroup recipe)
        {
            //If name is empty also add name
            if (string.IsNullOrEmpty(GroupName))
            {
                GroupName = recipe.GroupName;
            }
            
            EffectsToExecute.AddRange(recipe.EffectsToExecute);
        }
        

        /// <summary>
        /// Replace the list in this Description with the ones from a recipe.
        /// </summary>
        /// <param name="recipe"></param>
        public void ReplaceEffectsWithRecipe(GameFeelRecipe recipe)
        {
            //Use name from recipe.
            GroupName = recipe.Name;
            
            //Override effects with the ones from recipe
            EffectsToExecute = new List<GameFeelEffect>(recipe.effects);
        }
        
        public void ReplaceEffectsWithRecipe(GameFeelEffectGroup recipe)
        {
            //Copy values
            GroupName = recipe.GroupName;
            Disabled = recipe.Disabled;
            UnscaledTime = recipe.UnscaledTime;
            StepThroughMode = recipe.StepThroughMode;
            AppliesTo = recipe.AppliesTo;
            TargetTag = recipe.TargetTag;
            TargetList = recipe.TargetList;
            TargetComponentType = recipe.TargetComponentType;
            ExecuteOnTargetCopy = recipe.ExecuteOnTargetCopy;
            DisableRendererAndFollowOriginal = recipe.DisableRendererAndFollowOriginal;
            FollowEasing = recipe.FollowEasing;
            
            //Override effects with the ones from recipe
            EffectsToExecute = new List<GameFeelEffect>(recipe.EffectsToExecute);
        }
        

        public GameFeelRecipe SaveToRecipe(bool saveToFile = false, string description = "", string filename = null)
        {
            var recipe = new GameFeelRecipe {Name = GroupName, Description = description, effects = EffectsToExecute};
#if UNITY_EDITOR

            if (!saveToFile) return recipe;
            
            if (filename == null)
            {
                Debug.LogError("You must provide a filename when saving Recipe to file.");
                return recipe;
            }

            var json = JsonUtility.ToJson(recipe, true);

            if (!AssetDatabase.IsValidFolder(GameFeelDescription.savePath))
            {
                EditorHelpers.CreateFolder(GameFeelDescription.savePath);
            }

            using (var writer = new StreamWriter(Path.Combine(GameFeelDescription.savePath, filename), false))
            {
                writer.Write(json);
                writer.Close();
            }
#endif
            return recipe;
        }
        
        public void SaveToEffectGroup(bool saveToFile = false, string filename = null)
        {
#if UNITY_EDITOR
            if (saveToFile)
            {
                if (filename == null)
                {
                    Debug.LogError("You must provide a filename when saving Recipe to file.");
                    return;
                }

                var json = JsonUtility.ToJson(this, true);

                if (!AssetDatabase.IsValidFolder(GameFeelDescription.savePath))
                {
                    EditorHelpers.CreateFolder(GameFeelDescription.savePath);
                }

                using (var writer = new StreamWriter(Path.Combine(GameFeelDescription.savePath, filename), false))
                {
                    writer.Write(json);
                    writer.Close();
                }
            }
#endif
        }
        
        public static GameFeelRecipe LoadRecipeFromJson(string json)
        {
            return JsonUtility.FromJson<GameFeelRecipe>(json);
        }
        
        public static GameFeelEffectGroup LoadEffectGroupFromJson(string json)
        {
            return JsonUtility.FromJson<GameFeelEffectGroup>(json);
        }
    }
}