using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace SpaceShared
{
    /// <summary>Provides utility methods to migrate custom items saved through PyTK.</summary>
    internal class PyTkMigrator
    {
        /*********
        ** Public methods
        *********/
        /****
        ** Migrations
        ****/
        /// <summary>Migrate all items in the world which match the custom type.</summary>
        /// <param name="type">The custom type identifier.</param>
        /// <param name="getReplacement">Get the replacement for the given PyTK fields.</param>
        public static void MigrateItems(string type, Func<IDictionary<string, string>, Item> getReplacement)
        {
            // player items
            foreach (var player in Game1.getAllFarmers())
                PyTkMigrator.TryMigrate(player.Items, type, getReplacement);

            // items in locations
            foreach (GameLocation location in CommonHelper.GetLocations())
            {
                foreach ((Vector2 key, SObject obj) in location.Objects.Pairs.ToArray())
                {
                    if (PyTkMigrator.TryMigrate(obj, type, getReplacement, out Item replaceWith) && replaceWith is SObject replaceWithObj)
                        location.Objects[key] = replaceWithObj;
                }

                foreach (StorageFurniture furniture in location.furniture.OfType<StorageFurniture>())
                    PyTkMigrator.TryMigrate(furniture.heldItems, type, getReplacement);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Replace matched custom items found starting from the given items.</summary>
        /// <param name="items">The items to scan.</param>
        /// <param name="type">The custom type identifier.</param>
        /// <param name="getReplacement">Get the replacement for the given PyTK fields.</param>
        private static void TryMigrate(IList<Item> items, string type, Func<IDictionary<string, string>, Item> getReplacement)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (PyTkMigrator.TryMigrate(items[i], type, getReplacement, out Item newItem))
                    items[i] = newItem;
            }
        }

        /// <summary>Replace matched custom items found starting from the given item.</summary>
        /// <param name="item">The item to scan.</param>
        /// <param name="type">The custom type identifier.</param>
        /// <param name="getReplacement">Get the replacement for the given PyTK fields.</param>
        /// <param name="replaceWith">The new item, if applicable.</param>
        /// <returns>Returns whether the item should be replaced with <paramref name="replaceWith"/>.</returns>
        private static bool TryMigrate(Item item, string type, Func<IDictionary<string, string>, Item> getReplacement, out Item replaceWith)
        {
            if (PyTkMigrator.TryParseSerializedString(item?.Name, out string actualType, out IDictionary<string, string> customData) && actualType == type)
            {
                replaceWith = getReplacement(customData);
                return true;
            }

            if (item is Chest chest)
                PyTkMigrator.TryMigrate(chest.items, type, getReplacement);

            replaceWith = null;
            return false;
        }

        /// <summary>Parse the serialized data string for a PyTK object.</summary>
        /// <param name="serialized">The serialized data string.</param>
        /// <param name="type">The object type that was serialized.</param>
        /// <param name="customData">The custom data attributes, if any.</param>
        private static bool TryParseSerializedString(string serialized, out string type, out IDictionary<string, string> customData)
        {
            // ignore if not a PyTK item
            if (serialized?.StartsWith("PyTK|Item|") == true != true)
            {
                type = null;
                customData = null;
                return false;
            }

            // parse
            string[] fields = serialized.Split('|');
            type = fields[2];
            customData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string field in fields.Skip(3))
            {
                string[] parts = field.Split('=', 2);

                customData[parts[0]] = parts.GetOrDefault(1);
            }

            return true;
        }
    }
}
