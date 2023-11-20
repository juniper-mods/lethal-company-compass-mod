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
using static UnityEngine.UI.Image;



namespace Compass
{

    public class NewBehaviourScript : MonoBehaviour
    {
        public LinkedList<RectTransform> points;

        private float furthest_extent_left;
        private float furthest_extent_right;
        private float gap;
        private const float north = 359f;
        private bool initial_setup = false;

        public void Load(LinkedList<RectTransform> points)
        {
            this.points = points;
            furthest_extent_left = points.First().anchoredPosition.x;
            furthest_extent_right = points.Last().anchoredPosition.x;
            gap = points.First.Next.Value.anchoredPosition.x  - points.First().anchoredPosition.x;
        }
        private float previous_r = float.NaN;

        void Update()
        {
            if (!initial_setup)
            {
                try
                {
                    var camera_delta = north - GameNetworkManager.Instance.localPlayerController.cameraContainerTransform.eulerAngles.y; // angle delta
                    // Angle to pixels
                    var offset = Math.Abs(camera_delta / 90) * gap;
                    if (camera_delta < 0)
                    {
                        RotateLeft(offset);
                    }
                    else
                    {
                        RotateRight(offset);
                    }
                }
                catch (Exception ex) { return; }
                initial_setup = true;
            }
            var r = GameNetworkManager.Instance.localPlayerController.transform.eulerAngles.y;
            if (float.IsNaN(previous_r))
            {
                previous_r = r;
                return;
            }

            var delta = previous_r - r;
            var adj_delta = Math.Abs(delta / 90) * gap;

            if ( delta < 0) 
            {
                RotateLeft(adj_delta);
            } 
            else
            {
                RotateRight(adj_delta);
            }

            previous_r = r;
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
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LC_API.MyPluginInfo.PLUGIN_GUID)]

    public class Plugin : BaseUnityPlugin
    {
        static GameObject compass;
        static GameObject instance;
         
        void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.Patch(typeof(HUDManager).GetMethod("OnEnable",  BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic), postfix: new HarmonyMethod(typeof(Patch).GetMethod("Start")));
            harmony.Patch(typeof(HUDManager).GetMethod("OnDisable", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic), postfix: new HarmonyMethod(typeof(Patch).GetMethod("End")));
            harmony.Patch(typeof(HUDManager).GetMethod("HideHUD",   BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic), postfix: new HarmonyMethod(typeof(Patch).GetMethod("Toggle")));
            LC_API.BundleAPI.BundleLoader.OnLoadedAssets += () => {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID}->OnLoadedAssets is called!");
                compass = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<GameObject>("assets/LineCompassPlugin/Compass.prefab");
            };
        }

        
        class Patch
        {
            public static void Start()
            {
                Render();
            }
            public static void End()
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
            public static void Toggle(bool hide)
            {
                if (hide)
                {
                    End();
                }
                else
                {
                    Start();
                }
            }
        }

        static void Render()
        {
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
