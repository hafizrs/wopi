using SeliseBlocks.Genesis.Framework.PDS.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Extensions
{
    public static class ClassExtensions
    {
        public static List<FieldInfo> GetConstants(Type type)
        {
            FieldInfo[] fieldInfos = type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
            );

            var fields = fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();

            return fields;
        }

        public static List<string> GetPropertyNames(Type type, List<string> excluding = null)
        {
            List<string> propertyNames = new List<string>();

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (excluding is not null && excluding.Count != 0)
            {
                properties = properties.Where(x => !excluding.Contains(x.Name)).ToArray();
            }

            foreach (PropertyInfo property in properties)
            {
                propertyNames.Add(property.Name);
            }

            return propertyNames;
        }

        public static string GetBsonIdPropertyName<TEntity>()
        {
            if (InheritsFromBlocksEntityBase<TEntity>())
                return nameof(SeliseBlocks.Genesis.Framework.PDS.Entity.EntityBase.ItemId);

            return nameof(EntityBase.ItemId);
        }

        public static bool InheritsFromBlocksEntityBase<TEntity>()
        {
            Type entityType = typeof(TEntity);
            Type entityBaseType = typeof(SeliseBlocks.Genesis.Framework.PDS.Entity.EntityBase);

            return entityBaseType.IsAssignableFrom(entityType);
        }
    }
}
