using System.Xml;
using UnityEditor;
using UnityEngine;

namespace GameFeelDescriptions
{
    public class FullscreenFlashEffect : ColorChangeEffect
    {
        public FullscreenFlashEffect()
        {
            Description = "A fullscreen Camera flash.";

            loopType = LoopType.Yoyo;
            repeat = 1;
            Duration = 0.1f;
            setFromValue = true;
            @from = Color.clear;
        }
        
        [Tooltip("No reference, makes the effect lookup a camera on the target or the main camera.")]
        public Camera cameraToModify;  
        
        public Texture flashTexture = Texture2D.whiteTexture;

        private GameObject flash;
        
        public override GameFeelEffect CopyAndSetElapsed(GameObject origin, GameObject target, bool unscaledTime,
            Vector3? interactionDirection = null)
        {
            var cp = new FullscreenFlashEffect{cameraToModify = cameraToModify};
            cp.Init(origin, target, unscaledTime, interactionDirection);

            //Handling the cameraToModify setup here, to be able to a better use CompareTo
            if(cp.cameraToModify == null)
            {
                if (cp.target == null)
                {
                    cp.cameraToModify = Camera.main;
                }
                else
                {
                    cp.cameraToModify = cp.target.GetComponent<Camera>();
                    if(cp.cameraToModify == null)
                    {
                        cp.cameraToModify = Camera.main;
                    }    
                }
            }
           
            return DeepCopy(cp);
        }
        
        protected override void SetValue(GameObject target, Color value)
        {
            if (cameraToModify == null) return;
            
            flash.GetComponent<Renderer>().material.color = value;
        }

        protected override Color GetValue(GameObject target)
        {
            if(cameraToModify == null) return Color.magenta;
            
            return flash.GetComponent<Renderer>().material.color;
        }

        public override bool CompareTo(GameFeelEffect other)
        {
            return other is FullscreenFlashEffect cam && cam.cameraToModify == cameraToModify;
        }

        protected override void ExecuteSetup()
        {
            flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flash.name = "Flash";
            flash.transform.parent = GameFeelEffectExecutor.Instance.transform;
            //Put the flash right in front of the near clipping plane. 
            flash.transform.position = cameraToModify.transform.position + cameraToModify.transform.forward * cameraToModify.nearClipPlane;

            if (cameraToModify.orthographic)
            { 
                var ratio = 1.0f * Screen.height / Screen.width;
                flash.transform.localScale = Vector3.one * (1f / (ratio * 0.5f / cameraToModify.orthographicSize)); 
            }
            
            var rend = flash.GetComponent<Renderer>();
            rend.material =  new Material(Shader.Find("UI/Unlit/Transparent"));
            rend.material.mainTexture = flashTexture;
            rend.material.color = Color.clear;

            target = flash;
            this.OnComplete(new DestroyEffect());
            
            base.ExecuteSetup();
        }

        protected override bool TickTween()
        {
            if(cameraToModify == null) return true;
            
            SetValue(target, TweenHelper.Interpolate(start, elapsed / duration, end, GetEaseFunc()));

            return false;
        }
    }
}