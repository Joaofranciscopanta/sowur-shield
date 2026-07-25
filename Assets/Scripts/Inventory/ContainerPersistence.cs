using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Inventory
{
    /// <summary>
    /// One save/load path for every non-player container.
    ///
    /// SellBox and FeedingTrough each hand-wrote a per-slot loop into
    /// <c>worldStrings["..._slot3_item"]</c> / <c>worldCounters["..._slot3_qty"]</c>, while
    /// <see cref="InventoryContainer.GetSaveData"/> and <c>LoadFromSaveData</c> already existed
    /// and neither of them used it. This is that plumbing, in one place: a chest or a crafting
    /// bench added later persists by calling these two methods and nothing else.
    ///
    /// Introduced with save version 2 (Etapa 5 of review/04_CONTAINER_REFACTOR_PLAN.md).
    /// </summary>
    public static class ContainerPersistence
    {
        /// <summary>
        /// Write a container's contents into the save. The container's own
        /// <see cref="InventoryContainer.ContainerID"/> is the key, so it must be unique per
        /// instance — <c>$"SellBox_{gameObject.name}"</c> rather than a bare <c>"SellBox"</c>.
        /// </summary>
        public static void Save(GameData gameData, InventoryContainer container)
        {
            if (gameData == null || container == null) return;

            if (gameData.containerData == null)
                gameData.containerData = new ContainerCollectionData();

            if (string.IsNullOrEmpty(container.ContainerID))
            {
                Debug.LogWarning("[ContainerPersistence] Container has no ContainerID — not saved.");
                return;
            }

            gameData.containerData.Store(container.GetSaveData());
        }

        /// <summary>
        /// Restore a container's contents from the save.
        /// </summary>
        /// <returns>
        /// True if saved data was found and applied. False means this container has never been
        /// saved — a new game, or a v1 save where the contents were dropped by the migration.
        /// Callers should leave the container as-is in that case, not clear it.
        /// </returns>
        public static bool Load(GameData gameData, InventoryContainer container)
        {
            if (gameData?.containerData == null || container == null) return false;
            if (string.IsNullOrEmpty(container.ContainerID)) return false;

            InventoryContainer.ContainerSaveData data =
                gameData.containerData.Find(container.ContainerID);

            if (data == null) return false;

            container.LoadFromSaveData(data);
            return true;
        }
    }
}
