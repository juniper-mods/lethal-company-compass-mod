using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using LC_API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;



namespace MyFirstPlugin
{

    public class NewBehaviourScript : MonoBehaviour
    {
        public LinkedList<RectTransform> points;

        private bool left_rotation = true;
        private int i = 0;

        private float furthest_extent_left;
        private float furthest_extent_right;
        private float gap;

        public void Load(LinkedList<RectTransform> points)
        {
            this.points = points;
            furthest_extent_left = points.First().anchoredPosition.x;
            furthest_extent_right = points.Last().anchoredPosition.x;
            gap = points.First.Next.Value.anchoredPosition.x  - points.First().anchoredPosition.x;
            RotateRight(gap*2.5f);
        }
        private float previous_r = float.NaN;
          // Update is called once per frame
        void Update()
        {
            var r = GameNetworkManager.Instance.localPlayerController.transform.eulerAngles.y;
            if (float.IsNaN(previous_r))
            {
                previous_r = r;
                return;
            }

            var delta = previous_r - r;
            var adj_delta = Math.Abs(delta / 90) * gap;
            Debug.Log(String.Format("Delta: {0} - Adj: {1}", delta, adj_delta));

            if ( delta < 0) 
            {
                RotateLeft(adj_delta);
            } 
            else
            {
                RotateRight(adj_delta);
            }

            previous_r = r;
            if (!Moving()) { return; }

        }

        void RotateLeft(float amount)
        {
            if (amount == 0) { return; }
            foreach (RectTransform p in points)
            {
                p.anchoredPosition += Vector2.left * amount; // * Time.deltaTime;
            }

            bool extent = false;
            while (true)
            {
                extent = false;
                var first = points.First();
                if (first.anchoredPosition.x <= furthest_extent_left - gap)
                {
                    extent = true;
                    var last = points.Last().anchoredPosition;
                    first.anchoredPosition = new Vector2(last.x + gap, last.y);
                    points.RemoveFirst();
                    points.AddLast(first);
                    extent = true;
                }
                if (!extent) break;
            }
        }
        void RotateRight(float amount)
        {
            if (amount == 0) { return; }
            foreach (RectTransform p in points)
            {
                p.anchoredPosition += Vector2.right * amount; // * Time.deltaTime;
            }


            bool extent = false;
            while (true)
            {
                extent = false;
                var last = points.Last();
                if (last.anchoredPosition.x >= furthest_extent_right + gap)
                {
                    var first = points.First().anchoredPosition;
                    last.anchoredPosition = new Vector2(first.x - gap, first.y);
                    points.RemoveLast();
                    points.AddFirst(last);
                    extent = true;
                }
                if (!extent) break;
            }
        }

        bool Moving() { return true; }
        bool TurningLeft() {  return left_rotation; }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LC_API.MyPluginInfo.PLUGIN_GUID)]

    public class Plugin : BaseUnityPlugin
    {
        static GameObject compass;
        static GameObject instance;
        static BepInEx.Logging.ManualLogSource logger;
         
        void Awake()
        {
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            var harmonymain = new Harmony(PluginInfo.PLUGIN_GUID);
            harmonymain.PatchAll();
            LC_API.BundleAPI.BundleLoader.OnLoadedAssets += () => {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID}->OnLoadedAssets is called!");
                compass = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<GameObject>("assets/canvas.prefab");
            };
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        class Patch
        {
            static void Prefix()
            {
                Render();
            }
        }

  /*      [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPrefix]
        public static void openingDoorsSequence()
        {
            Render();
        }*/

        static void Render()
        {
            Debug.LogError("RENDER HAS BEEN CALLED");
            var _compass = UnityEngine.Object.Instantiate(compass);
            _compass.layer = LayerMask.NameToLayer("UI");
            var bounding_box = _compass.transform.Find("Image").GetChild(0).gameObject;
            var points = new List<RectTransform>();
            for (int i = 0; i < bounding_box.transform.childCount; i++)
            {
                var child = bounding_box.transform.GetChild(i).gameObject;
                var textMesh = child.GetComponent<TextMeshProUGUI>();
                textMesh.text = child.name;
                points.Add(child.GetComponent<RectTransform>());

            }
            var nbs = _compass.AddComponent<NewBehaviourScript>();
            nbs.Load(new LinkedList<RectTransform>(points));
            instance = _compass;
        }
    }
}
