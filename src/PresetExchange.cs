using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PromptPalette
{
    [DataContract]
    internal sealed class ExchangeSubcategory
    {
        public ExchangeSubcategory()
        {
            Name = string.Empty;
        }

        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    internal sealed class ExchangeCategory
    {
        public ExchangeCategory()
        {
            Name = string.Empty;
            Subcategories = new List<ExchangeSubcategory>();
        }

        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; }

        [DataMember(Name = "subcategories", Order = 2)]
        public List<ExchangeSubcategory> Subcategories { get; set; }
    }

    [DataContract]
    internal sealed class ExchangeMembership
    {
        public ExchangeMembership()
        {
            Category = string.Empty;
            Subcategory = string.Empty;
        }

        [DataMember(Name = "category", Order = 1)]
        public string Category { get; set; }

        [DataMember(Name = "subcategory", Order = 2)]
        public string Subcategory { get; set; }
    }

    [DataContract]
    internal sealed class ExchangeTag
    {
        public ExchangeTag()
        {
            Chinese = string.Empty;
            English = string.Empty;
            Memberships = new List<ExchangeMembership>();
        }

        [DataMember(Name = "chinese", Order = 1)]
        public string Chinese { get; set; }

        [DataMember(Name = "english", Order = 2)]
        public string English { get; set; }

        [DataMember(Name = "memberships", Order = 3)]
        public List<ExchangeMembership> Memberships { get; set; }
    }

    [DataContract]
    internal sealed class PresetExchange
    {
        public PresetExchange()
        {
            Format = PresetExchangeIO.FormatName;
            Version = PresetExchangeIO.FormatVersion;
            Name = string.Empty;
            Categories = new List<ExchangeCategory>();
            Tags = new List<ExchangeTag>();
        }

        [DataMember(Name = "format", Order = 1)]
        public string Format { get; set; }

        [DataMember(Name = "version", Order = 2)]
        public int Version { get; set; }

        [DataMember(Name = "name", Order = 3)]
        public string Name { get; set; }

        [DataMember(Name = "categories", Order = 4)]
        public List<ExchangeCategory> Categories { get; set; }

        [DataMember(Name = "tags", Order = 5)]
        public List<ExchangeTag> Tags { get; set; }
    }

    internal sealed class ImportedPreset
    {
        public string Name;
        public PromptLibrary Library;
        public int CategoryCount;
        public int SubcategoryCount;
        public int TagCount;
    }

    internal sealed class MergeStats
    {
        public int NewCategories;
        public int NewSubcategories;
        public int NewTags;
        public int UpdatedTags;
        public int AddedMemberships;

        public string Summary()
        {
            return "新增分类 " + NewCategories + "、小分类 " + NewSubcategories
                + "、标签 " + NewTags + "；更新共享标签 " + UpdatedTags
                + "（新增归属 " + AddedMemberships + "）";
        }
    }

    internal static class PresetExchangeIO
    {
        public const string FormatName = "prompt-palette-preset";
        public const int FormatVersion = 1;

        public static PresetExchange BuildExchange(PromptLibrary library, string presetName)
        {
            PresetExchange exchange = new PresetExchange();
            exchange.Name = presetName ?? string.Empty;
            for (int c = 0; c < library.Categories.Count; c++)
            {
                PromptCategory category = library.Categories[c];
                ExchangeCategory exchangeCategory = new ExchangeCategory();
                exchangeCategory.Name = category.Name;
                for (int s = 0; s < category.Subcategories.Count; s++)
                {
                    ExchangeSubcategory sub = new ExchangeSubcategory();
                    sub.Name = category.Subcategories[s].Name;
                    exchangeCategory.Subcategories.Add(sub);
                }
                exchange.Categories.Add(exchangeCategory);
            }

            for (int t = 0; t < library.Tags.Count; t++)
            {
                PromptTag tag = library.Tags[t];
                ExchangeTag exchangeTag = new ExchangeTag();
                exchangeTag.Chinese = tag.Chinese;
                exchangeTag.English = tag.English;
                for (int m = 0; m < tag.SubcategoryIds.Count; m++)
                {
                    PromptCategory parent;
                    PromptSubcategory sub = library.FindSubcategory(tag.SubcategoryIds[m], out parent);
                    if (sub == null) continue;
                    ExchangeMembership membership = new ExchangeMembership();
                    membership.Category = parent.Name;
                    membership.Subcategory = sub.Name;
                    exchangeTag.Memberships.Add(membership);
                }
                exchange.Tags.Add(exchangeTag);
            }
            return exchange;
        }

        public static void WriteFile(string path, PresetExchange exchange)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            using (FileStream stream = File.Create(path))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(PresetExchange));
                serializer.WriteObject(stream, exchange);
            }
        }

        public static bool ReadFile(string path, out ImportedPreset imported, out string error)
        {
            imported = null;
            error = null;
            try
            {
                string json;
                using (StreamReader reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    json = reader.ReadToEnd();
                }
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                PresetExchange exchange;
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(PresetExchange));
                    exchange = (PresetExchange)serializer.ReadObject(stream);
                }
                if (exchange == null)
                {
                    error = "文件内容为空。";
                    return false;
                }
                return BuildImportedPreset(exchange, out imported, out error);
            }
            catch (Exception ex)
            {
                error = "无法读取预设文件：" + ex.Message;
                return false;
            }
        }

        public static bool BuildImportedPreset(PresetExchange exchange,
            out ImportedPreset imported, out string error)
        {
            imported = null;
            error = null;
            if (exchange.Format != FormatName)
            {
                error = "format 必须为 \"" + FormatName + "\"。";
                return false;
            }
            if (exchange.Version != FormatVersion)
            {
                error = "不支持的 version：" + exchange.Version + "，当前只支持 1。";
                return false;
            }
            string presetName = (exchange.Name ?? string.Empty).Trim();
            if (presetName.Length == 0 || presetName.Length > 80)
            {
                error = "预设名称不能为空，且不能超过 80 个字符。";
                return false;
            }

            List<ExchangeCategory> categories = exchange.Categories ?? new List<ExchangeCategory>();
            List<ExchangeTag> tags = exchange.Tags ?? new List<ExchangeTag>();
            if (categories.Count > 100)
            {
                error = "分类数量不能超过 100。";
                return false;
            }
            if (tags.Count > 10000)
            {
                error = "标签数量不能超过 10000。";
                return false;
            }

            PromptLibrary library = new PromptLibrary();
            Dictionary<string, string> subcategoryIds =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> categoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int subcategoryCount = 0;

            for (int c = 0; c < categories.Count; c++)
            {
                ExchangeCategory sourceCategory = categories[c];
                if (sourceCategory == null)
                {
                    error = "categories[" + c + "] 不能为 null。";
                    return false;
                }
                string categoryName = (sourceCategory.Name ?? string.Empty).Trim();
                if (categoryName.Length == 0 || categoryName.Length > 40)
                {
                    error = "第 " + (c + 1) + " 个分类的名称不能为空，且不能超过 40 个字符。";
                    return false;
                }
                if (categoryNames.Contains(categoryName))
                {
                    error = "分类名称重复：" + categoryName;
                    return false;
                }
                categoryNames.Add(categoryName);
                PromptCategory category = new PromptCategory(PromptLibrary.NewId("cat"), categoryName);
                library.Categories.Add(category);

                List<ExchangeSubcategory> subs =
                    sourceCategory.Subcategories ?? new List<ExchangeSubcategory>();
                if (subs.Count > 100)
                {
                    error = "分类「" + categoryName + "」的小分类数量不能超过 100。";
                    return false;
                }
                HashSet<string> subNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int s = 0; s < subs.Count; s++)
                {
                    ExchangeSubcategory sourceSub = subs[s];
                    if (sourceSub == null)
                    {
                        error = "分类「" + categoryName + "」的小分类不能为 null。";
                        return false;
                    }
                    string subName = (sourceSub.Name ?? string.Empty).Trim();
                    if (subName.Length == 0 || subName.Length > 40)
                    {
                        error = "分类「" + categoryName + "」中有小分类名称为空或超过 40 个字符。";
                        return false;
                    }
                    if (subNames.Contains(subName))
                    {
                        error = "分类「" + categoryName + "」中小分类名称重复：" + subName;
                        return false;
                    }
                    subNames.Add(subName);
                    PromptSubcategory sub = new PromptSubcategory(PromptLibrary.NewId("sub"), subName);
                    category.Subcategories.Add(sub);
                    subcategoryIds[categoryName + "\u0001" + subName] = sub.Id;
                    subcategoryCount++;
                }
            }

            HashSet<string> tagKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int validTags = 0;
            for (int t = 0; t < tags.Count; t++)
            {
                ExchangeTag sourceTag = tags[t];
                if (sourceTag == null)
                {
                    error = "tags[" + t + "] 不能为 null。";
                    return false;
                }
                string chinese = (sourceTag.Chinese ?? string.Empty).Trim();
                string english = (sourceTag.English ?? string.Empty).Trim();
                if (chinese.Length == 0 || chinese.Length > 60)
                {
                    error = "第 " + (t + 1) + " 个标签的 chinese 不能为空，且不能超过 60 个字符。";
                    return false;
                }
                if (english.Length > 120)
                {
                    error = "标签「" + chinese + "」的 english 不能超过 120 个字符。";
                    return false;
                }
                string key = TagIdentity(chinese, english);
                if (tagKeys.Contains(key))
                {
                    error = "标签重复：中文「" + chinese + "」/ English「" + english
                        + "」。同一对中英文只能出现一次；跨分类请使用 memberships 多归属。";
                    return false;
                }
                tagKeys.Add(key);

                PromptTag tag = new PromptTag(PromptLibrary.NewId("tag"), chinese, chinese, english);
                List<ExchangeMembership> memberships =
                    sourceTag.Memberships ?? new List<ExchangeMembership>();
                HashSet<string> usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int m = 0; m < memberships.Count; m++)
                {
                    ExchangeMembership membership = memberships[m];
                    if (membership == null)
                    {
                        error = "标签「" + chinese + "」的 memberships 不能包含 null。";
                        return false;
                    }
                    string category = (membership.Category ?? string.Empty).Trim();
                    string subcategory = (membership.Subcategory ?? string.Empty).Trim();
                    if (category.Length == 0 || subcategory.Length == 0)
                    {
                        error = "标签「" + chinese + "」的归属路径中 category/subcategory 不能为空。";
                        return false;
                    }
                    string path = category + "\u0001" + subcategory;
                    if (usedPaths.Contains(path))
                    {
                        error = "标签「" + chinese + "」重复声明了归属：" + category + " / " + subcategory;
                        return false;
                    }
                    usedPaths.Add(path);
                    string subId;
                    if (!subcategoryIds.TryGetValue(path, out subId))
                    {
                        error = "标签「" + chinese + "」引用了不存在的归属路径："
                            + category + " / " + subcategory;
                        return false;
                    }
                    tag.SubcategoryIds.Add(subId);
                }
                library.Tags.Add(tag);
                validTags++;
            }

            library.Normalize();
            imported = new ImportedPreset();
            imported.Name = presetName;
            imported.Library = library;
            imported.CategoryCount = library.Categories.Count;
            imported.SubcategoryCount = subcategoryCount;
            imported.TagCount = validTags;
            return true;
        }

        public static string TagIdentity(string chinese, string english)
        {
            return (chinese ?? string.Empty).Trim() + "\u0001"
                + (english ?? string.Empty).Trim().ToLowerInvariant();
        }
    }

    internal static class PresetMerger
    {
        public static MergeStats Merge(PromptLibrary target, PromptLibrary source)
        {
            MergeStats stats = new MergeStats();
            if (target == null || source == null) return stats;

            Dictionary<string, string> subcategoryMap =
                new Dictionary<string, string>(StringComparer.Ordinal);
            for (int c = 0; c < source.Categories.Count; c++)
            {
                PromptCategory sourceCategory = source.Categories[c];
                PromptCategory targetCategory = FindCategory(target, sourceCategory.Name);
                if (targetCategory == null)
                {
                    targetCategory = target.AddCategory(sourceCategory.Name);
                    stats.NewCategories++;
                }

                for (int s = 0; s < sourceCategory.Subcategories.Count; s++)
                {
                    PromptSubcategory sourceSub = sourceCategory.Subcategories[s];
                    PromptSubcategory targetSub = FindSubcategory(targetCategory, sourceSub.Name);
                    if (targetSub == null)
                    {
                        targetSub = target.AddSubcategory(targetCategory, sourceSub.Name);
                        stats.NewSubcategories++;
                    }
                    subcategoryMap[sourceSub.Id] = targetSub.Id;
                }
            }

            Dictionary<string, PromptTag> targetTags =
                new Dictionary<string, PromptTag>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < target.Tags.Count; i++)
            {
                PromptTag tag = target.Tags[i];
                string key = PresetExchangeIO.TagIdentity(tag.Chinese, tag.English);
                if (!targetTags.ContainsKey(key)) targetTags[key] = tag;
            }

            for (int t = 0; t < source.Tags.Count; t++)
            {
                PromptTag sourceTag = source.Tags[t];
                string key = PresetExchangeIO.TagIdentity(sourceTag.Chinese, sourceTag.English);
                PromptTag targetTag;
                bool exists = targetTags.TryGetValue(key, out targetTag);
                if (!exists)
                {
                    targetTag = new PromptTag(
                        PromptLibrary.NewId("tag"), sourceTag.Chinese,
                        sourceTag.Chinese, sourceTag.English);
                    target.Tags.Add(targetTag);
                    targetTags[key] = targetTag;
                    stats.NewTags++;
                }

                bool updated = false;
                for (int m = 0; m < sourceTag.SubcategoryIds.Count; m++)
                {
                    string mapped;
                    if (!subcategoryMap.TryGetValue(sourceTag.SubcategoryIds[m], out mapped)) continue;
                    if (targetTag.SubcategoryIds.Contains(mapped)) continue;
                    targetTag.SubcategoryIds.Add(mapped);
                    stats.AddedMemberships++;
                    updated = true;
                }
                if (exists && updated) stats.UpdatedTags++;
            }
            return stats;
        }

        private static PromptCategory FindCategory(PromptLibrary library, string name)
        {
            for (int i = 0; i < library.Categories.Count; i++)
            {
                if (string.Equals(library.Categories[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return library.Categories[i];
            }
            return null;
        }

        private static PromptSubcategory FindSubcategory(PromptCategory category, string name)
        {
            for (int i = 0; i < category.Subcategories.Count; i++)
            {
                if (string.Equals(category.Subcategories[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return category.Subcategories[i];
            }
            return null;
        }
    }
}
