using Sitecore.Data;
using Sitecore.Caching;
using Sitecore.Collections;
using Sitecore.Common;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.LanguageFallback;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using System;

namespace Sitecore.Support.Data
{
    public class StandardValuesProvider : Sitecore.Data.StandardValuesProvider
    {
        public override string GetStandardValue(Field field)
        {
            Assert.ArgumentNotNull(field, "field");
            if (field.ID == FieldIDs.SourceItem || field.ID == FieldIDs.Source)
            {
                return string.Empty;
            }

            SafeDictionary<ID, string> standardValues = this.GetStandardValues(field.Item);
            if (standardValues == null)
            {
                return null;
            }
            return standardValues[field.ID];
        }

        private SafeDictionary<ID, string> GetStandardValues(Item item)
        {
            if (ID.IsNullOrEmpty(item.TemplateID))
            {
                return null;
            }
            SafeDictionary<ID, string> safeDictionary = this.GetStandardValuesFromCache(item);
            if (safeDictionary != null)
            {
                return safeDictionary;
            }
            safeDictionary = this.ReadStandardValues(item.TemplateID, item.Database, item.OriginalLanguage);
            this.AddStandardValuesToCache(item, safeDictionary);
            return safeDictionary;
        }

        private SafeDictionary<ID, string> GetStandardValuesFromCache(Item item)
        {
            StandardValuesCache standardValuesCache = item.Database.Caches.StandardValuesCache;

            #region Bug# 119290 Modified code
            if (item.IsFallback && LanguageFallbackItemSwitcher.CurrentValue == null)
            {
                Item fallbackItem = item.GetFallbackItem();
                while (fallbackItem != null && fallbackItem.GetFallbackItem() != null && Context.Database.GetItem(fallbackItem.ID, fallbackItem.Language) == null)
                {
                    fallbackItem = fallbackItem.GetFallbackItem();
                }
                return item.Database.Caches.StandardValuesCache.GetStandardValues(fallbackItem ?? item);
            }
            #endregion

            return item.Database.Caches.StandardValuesCache.GetStandardValues(item);
        }

        private void AddStandardValuesToCache(Item item, SafeDictionary<ID, string> values)
        {
            StandardValuesCache standardValuesCache = item.Database.Caches.StandardValuesCache;
            standardValuesCache.AddStandardValues(item, values);
        }

        private SafeDictionary<ID, string> ReadStandardValues(ID templateId, Database database, Language language)
        {
            SafeDictionary<ID, string> result = new SafeDictionary<ID, string>();
            Template template = TemplateManager.GetTemplate(templateId, database);
            if (template == null)
            {
                return result;
            }
            this.AddStandardValues(template, database, language, result);
            TemplateList baseTemplates = template.GetBaseTemplates();
            foreach (Template template2 in baseTemplates)
            {
                this.AddStandardValues(template2, database, language, result);
            }
            return result;
        }

        private void AddStandardValues(Template template, Database database, Language language, SafeDictionary<ID, string> result)
        {
            ID standardValueHolderId = template.StandardValueHolderId;
            if (ID.IsNullOrEmpty(standardValueHolderId))
            {
                return;
            }
            bool? currentValue = Switcher<bool?, LanguageFallbackItemSwitcher>.CurrentValue;
            Item item;
            if (currentValue == false)
            {
                try
                {
                    Switcher<bool?, LanguageFallbackItemSwitcher>.Exit();
                    item = ItemManager.GetItem(standardValueHolderId, language, Sitecore.Data.Version.Latest, database, SecurityCheck.Disable);
                    goto IL_5A;
                }
                finally
                {
                    Switcher<bool?, LanguageFallbackItemSwitcher>.Enter(currentValue);
                }
            }
            item = ItemManager.GetItem(standardValueHolderId, language, Sitecore.Data.Version.Latest, database, SecurityCheck.Disable);
        IL_5A:
            if (item == null)
            {
                return;
            }
            foreach (Field field in item.Fields)
            {
                if (!result.ContainsKey(field.ID))
                {
                    string value = field.GetValue(false, true);
                    if (value != null)
                    {
                        result[field.ID] = value;
                    }
                }
            }
            if (!result.ContainsKey(FieldIDs.StandardValueHolderId))
            {
                result[FieldIDs.StandardValueHolderId] = standardValueHolderId.ToString();
            }
        }
    }
}