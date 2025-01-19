using DV.CashRegister;
using DV.Customization.Paint;
using DV.Shops;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkinManagerMod.Items
{
    public class ShopPaintCanStocker : MonoBehaviour
    {
        private const int NUM_THEMES_TO_STOCK = 10;

        public Shop Shop;
        public CashRegisterWithModules CashRegister;

        private GameObject _scanModulePrototype;
        private readonly List<ScanItemCashRegisterModule> _injectedModules = new List<ScanItemCashRegisterModule>(NUM_THEMES_TO_STOCK);
        private ScanItemCashRegisterModule[] _preInjectionModules = null;

        public void Awake()
        {
            Shop = GetComponent<Shop>();
            CashRegister = GetComponentInChildren<CashRegisterWithModules>(true);

            _scanModulePrototype = GetComponentsInChildren<ScanItemCashRegisterModule>(true)
                .First(m => m.sellingItemSpec.itemPrefabName == PaintFactory.DEFAULT_CAN_PREFAB_NAME)
                .gameObject;
        }

        public void OnEnable()
        {
            var themes = SkinProvider.GetRandomizedStoreThemes();
            int nToStock = Math.Min(themes.Count, NUM_THEMES_TO_STOCK);

            if (nToStock == 0) return;

            _preInjectionModules = Shop.scanItemResourceModules;
            int originalModuleCount = _preInjectionModules.Length;

            var postInjectionModules = new ScanItemCashRegisterModule[_preInjectionModules.Length + nToStock];
            Array.Copy(_preInjectionModules, postInjectionModules, _preInjectionModules.Length);

            for (int i = 0; i < nToStock; i++)
            {
                var module = CreateModuleForTheme(themes[i]);
                _injectedModules.Add(module);

                postInjectionModules[originalModuleCount + i] = module;

                Main.LogVerbose($"Add shop module for theme {themes[i].name} to {gameObject.name}");
            }

            Shop.scanItemResourceModules = postInjectionModules;
            CashRegister.registerModules = postInjectionModules;
        }

        public void OnDisable()
        {
            Shop.scanItemResourceModules = _preInjectionModules;
            CashRegister.registerModules = _preInjectionModules;
            _preInjectionModules = null;

            foreach (var module in _injectedModules)
            {
                Destroy(module.gameObject);
            }
        }


        private ScanItemCashRegisterModule CreateModuleForTheme(PaintTheme theme)
        {
            if (!PaintFactory.ShopDataInjected)
            {
                PaintFactory.InjectShopData();
            }

            // scan module
            var moduleHolder = Instantiate(_scanModulePrototype, transform);
            PaintFactory.ApplyLabelMaterialToShelfItem(moduleHolder, theme.name);

            var newModule = moduleHolder.GetComponent<ScanItemCashRegisterModule>();
            newModule.sellingItemSpec = PaintFactory.GetDummyItemSpec(theme.name);

            return newModule;
        }
    }
}