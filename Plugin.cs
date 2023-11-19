﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using LC_API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



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
        }

          // Update is called once per frame
        void Update()
        {
            if (i % 250 == 0)
            { 
                if (i == 500) {
                    left_rotation = !left_rotation;
                    i = 0;
                }
                
                if (TurningLeft())
                {
                    RotateLeft(gap*123);
                }
                else
                {
                    RotateRight(gap * 123);
                }
            }
            i += 1;
            if (!Moving()) { return; }

        }

        void RotateLeft(float amount)
        {
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
    [HarmonyPatch]
    public class Plugin : BaseUnityPlugin
    {
        static GameObject compass;
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

                Render();
            };
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.openingDoorsSequence))]
        [HarmonyPrefix]
        public static void openingDoorsSequence()
        {
            Render();
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
        }
    }
}