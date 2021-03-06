using System;
using UnityEngine;

namespace GameFeelDescriptions
{
    [Serializable]
    public class EnableEffect : GameFeelEffect
    {
        public EnableEffect()
        {
            Description = "Enable game object, unless onlyRenderer and/or onlyCollider is set, in which case it'll enable collisions and/or rendering.";
        }

        public bool onlyCollider;
        public bool onlyRenderer;
      
        //TODO: maybe Remove this effect, replace all logic with copies of the thing that the effect happened to. 11/02/2020

        public override GameFeelEffect CopyAndSetElapsed(GameObject origin, GameObject target,
            GameFeelTriggerData triggerData, bool ignoreCooldown = false)
        {
            var cp = new EnableEffect{onlyCollider = onlyCollider, onlyRenderer = onlyRenderer};
            cp.Init(origin, target, triggerData);
            return DeepCopy(cp, ignoreCooldown);
        }

        protected override bool ExecuteTick()
        {       
            if (target == null) return true;
            
            if (onlyCollider)
            {
                var col = target.GetComponent<Collider>();
                if (col != null) col.enabled = true;
                
                var col2D = target.GetComponent<Collider2D>(); 
                if (col2D != null) col2D.enabled = true;
            }
            
            if (onlyRenderer)
            {
                var render = target.GetComponent<Renderer>();
                if (render != null) render.enabled = true;
            }
            
            if(!onlyCollider && !onlyRenderer)
            {
                target.SetActive(true);   
            }

            //We're done
            return true;
        }
    }
}