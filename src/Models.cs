using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PromptPalette
{
    internal enum PromptLanguage
    {
        Chinese = 0,
        English = 1
    }

    [DataContract]
    internal sealed class PromptSubcategory
    {
        public PromptSubcategory()
        {
            Id = string.Empty;
            Name = string.Empty;
        }

        public PromptSubcategory(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }
    }

    [DataContract]
    internal sealed class PromptCategory
    {
        public PromptCategory()
        {
            Id = string.Empty;
            Name = string.Empty;
            Subcategories = new List<PromptSubcategory>();
        }

        public PromptCategory(string id, string name)
        {
            Id = id;
            Name = name;
            Subcategories = new List<PromptSubcategory>();
        }

        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public List<PromptSubcategory> Subcategories { get; set; }
    }

    [DataContract]
    internal sealed class PromptTag
    {
        public PromptTag()
        {
            Id = string.Empty;
            Label = string.Empty;
            Chinese = string.Empty;
            English = string.Empty;
            SubcategoryIds = new List<string>();
        }

        public PromptTag(string id, string label, string chinese, string english)
        {
            Id = id;
            Label = label;
            Chinese = chinese;
            English = english;
            SubcategoryIds = new List<string>();
        }

        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Label { get; set; }

        [DataMember(Order = 3)]
        public string Chinese { get; set; }

        [DataMember(Order = 4)]
        public string English { get; set; }

        [DataMember(Order = 5)]
        public List<string> SubcategoryIds { get; set; }

        public string Prompt(PromptLanguage language)
        {
            if (language == PromptLanguage.English && !string.IsNullOrEmpty(English))
                return English;
            return Chinese;
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            return Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || Chinese.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || English.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    [DataContract]
    internal sealed class PromptLibrary
    {
        public PromptLibrary()
        {
            Version = 1;
            Categories = new List<PromptCategory>();
            Tags = new List<PromptTag>();
        }

        [DataMember(Order = 1)]
        public int Version { get; set; }

        [DataMember(Order = 2)]
        public List<PromptCategory> Categories { get; set; }

        [DataMember(Order = 3)]
        public List<PromptTag> Tags { get; set; }

        public PromptCategory FindCategory(string id)
        {
            for (int i = 0; i < Categories.Count; i++)
            {
                if (Categories[i].Id == id) return Categories[i];
            }
            return null;
        }

        public PromptSubcategory FindSubcategory(string id, out PromptCategory parent)
        {
            for (int c = 0; c < Categories.Count; c++)
            {
                PromptCategory category = Categories[c];
                for (int s = 0; s < category.Subcategories.Count; s++)
                {
                    if (category.Subcategories[s].Id == id)
                    {
                        parent = category;
                        return category.Subcategories[s];
                    }
                }
            }
            parent = null;
            return null;
        }

        public PromptSubcategory FindSubcategory(string id)
        {
            PromptCategory parent;
            return FindSubcategory(id, out parent);
        }

        public PromptTag FindTag(string id)
        {
            for (int i = 0; i < Tags.Count; i++)
            {
                if (Tags[i].Id == id) return Tags[i];
            }
            return null;
        }

        public PromptTag FindTagByChinese(string chinese)
        {
            for (int i = 0; i < Tags.Count; i++)
            {
                if (string.Equals(Tags[i].Chinese, chinese, StringComparison.OrdinalIgnoreCase))
                    return Tags[i];
            }
            return null;
        }

        public List<PromptTag> GetAllTags()
        {
            return Tags;
        }

        public List<PromptTag> GetTagsForCategory(PromptCategory category)
        {
            List<PromptTag> result = new List<PromptTag>();
            if (category == null) return result;
            HashSet<string> subIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < category.Subcategories.Count; i++)
            {
                subIds.Add(category.Subcategories[i].Id);
            }
            for (int i = 0; i < Tags.Count; i++)
            {
                PromptTag tag = Tags[i];
                for (int m = 0; m < tag.SubcategoryIds.Count; m++)
                {
                    if (subIds.Contains(tag.SubcategoryIds[m]))
                    {
                        result.Add(tag);
                        break;
                    }
                }
            }
            return result;
        }

        public List<PromptTag> GetTagsForSubcategory(string subcategoryId)
        {
            List<PromptTag> result = new List<PromptTag>();
            if (string.IsNullOrEmpty(subcategoryId)) return result;
            for (int i = 0; i < Tags.Count; i++)
            {
                PromptTag tag = Tags[i];
                if (tag.SubcategoryIds.Contains(subcategoryId)) result.Add(tag);
            }
            return result;
        }

        public int GetDistinctCategoryCount(PromptTag tag)
        {
            HashSet<string> categoryIds = new HashSet<string>(StringComparer.Ordinal);
            for (int c = 0; c < Categories.Count; c++)
            {
                PromptCategory category = Categories[c];
                for (int s = 0; s < category.Subcategories.Count; s++)
                {
                    if (tag.SubcategoryIds.Contains(category.Subcategories[s].Id))
                    {
                        categoryIds.Add(category.Id);
                        break;
                    }
                }
            }
            return categoryIds.Count;
        }

        public int GetSubcategoryCount(PromptTag tag)
        {
            return tag.SubcategoryIds == null ? 0 : tag.SubcategoryIds.Count;
        }

        public string GetMembershipText(PromptTag tag)
        {
            List<string> labels = new List<string>();
            for (int c = 0; c < Categories.Count; c++)
            {
                PromptCategory category = Categories[c];
                for (int s = 0; s < category.Subcategories.Count; s++)
                {
                    if (tag.SubcategoryIds.Contains(category.Subcategories[s].Id))
                    {
                        string label = category.Name + " / " + category.Subcategories[s].Name;
                        if (!labels.Contains(label)) labels.Add(label);
                    }
                }
            }
            if (labels.Count == 0) return "未归类";
            return string.Join("  ·  ", labels.ToArray());
        }

        public PromptCategory AddCategory(string name)
        {
            PromptCategory category = new PromptCategory(NewId("cat"), name.Trim());
            Categories.Add(category);
            return category;
        }

        public PromptSubcategory AddSubcategory(PromptCategory category, string name)
        {
            if (category == null) return null;
            PromptSubcategory subcategory = new PromptSubcategory(NewId("sub"), name.Trim());
            category.Subcategories.Add(subcategory);
            return subcategory;
        }

        public PromptTag AddTag(string chinese, string english, IEnumerable<string> subcategoryIds)
        {
            PromptTag tag = new PromptTag(NewId("tag"), chinese.Trim(), chinese.Trim(),
                (english ?? string.Empty).Trim());
            ApplyMemberships(tag, subcategoryIds);
            Tags.Add(tag);
            return tag;
        }

        public void UpdateTag(PromptTag tag, string chinese, string english, IEnumerable<string> subcategoryIds)
        {
            if (tag == null) return;
            tag.Label = chinese.Trim();
            tag.Chinese = chinese.Trim();
            tag.English = (english ?? string.Empty).Trim();
            ApplyMemberships(tag, subcategoryIds);
        }

        public void RemoveTag(PromptTag tag)
        {
            if (tag != null) Tags.Remove(tag);
        }

        private void ApplyMemberships(PromptTag tag, IEnumerable<string> subcategoryIds)
        {
            tag.SubcategoryIds = new List<string>();
            if (subcategoryIds == null) return;
            HashSet<string> added = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in subcategoryIds)
            {
                if (string.IsNullOrEmpty(id) || added.Contains(id)) continue;
                if (FindSubcategory(id) == null) continue;
                added.Add(id);
                tag.SubcategoryIds.Add(id);
            }
        }

        public void Normalize()
        {
            if (Categories == null) Categories = new List<PromptCategory>();
            if (Tags == null) Tags = new List<PromptTag>();
            if (Version <= 0) Version = 1;
            for (int c = 0; c < Categories.Count; c++)
            {
                PromptCategory category = Categories[c];
                if (category.Subcategories == null) category.Subcategories = new List<PromptSubcategory>();
            }
            for (int i = 0; i < Tags.Count; i++)
            {
                PromptTag tag = Tags[i];
                if (tag.SubcategoryIds == null) tag.SubcategoryIds = new List<string>();
                ApplyMemberships(tag, tag.SubcategoryIds);
            }
        }

        public static string NewId(string prefix)
        {
            return prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
