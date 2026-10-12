using System;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Helpers for moving Rust-flavoured items in and out of the player's PEAK inventory.
    /// Everything is guarded: if the inventory is full the item is dropped on the ground
    /// instead, and any failure is swallowed so a bad state never stops the game.
    /// </summary>
    public static class RustItems
    {
        /// <summary>How many of a Rust shortname the player is carrying in their quick slots.</summary>
        public static int Count(Character ch, string shortname)
        {
            if (ch == null || ch.player == null || string.IsNullOrEmpty(shortname)) return 0;
            ItemSlot[] slots = ch.player.itemSlots;
            if (slots == null) return 0;
            int n = 0;
            foreach (ItemSlot slot in slots)
            {
                if (slot == null || slot.IsEmpty()) continue;
                if (RustSwap.ShortnameForItem(slot.prefab) == shortname) n++;
            }
            return n;
        }

        /// <summary>Removes up to <paramref name="count"/> of a Rust shortname.</summary>
        public static int Take(Character ch, string shortname, int count)
        {
            if (ch == null || ch.player == null || count <= 0) return 0;
            ItemSlot[] slots = ch.player.itemSlots;
            if (slots == null) return 0;
            bool broadcast = ch.photonView != null && ch.photonView.IsMine;
            int taken = 0;
            foreach (ItemSlot slot in slots)
            {
                if (taken >= count) break;
                if (slot == null || slot.IsEmpty()) continue;
                if (RustSwap.ShortnameForItem(slot.prefab) != shortname) continue;
                try { ch.player.EmptySlot(Optionable<byte>.Some(slot.itemSlotID), broadcast); }
                catch { }
                taken++;
            }
            return taken;
        }

        /// <summary>Gives the player items, dropping any that do not fit on the ground.</summary>
        public static int Give(Character ch, string shortname, int count)
        {
            if (ch == null || count <= 0) return 0;
            Item prefab = RustSwap.PeakItemForShortname(shortname);
            if (prefab == null) return 0;

            int given = 0;
            for (int i = 0; i < count; i++)
            {
                if (TryAddToInventory(ch, prefab)) given++;
                else if (SpawnOnGround(ch, prefab, i)) given++;
            }
            return given;
        }

        private static bool TryAddToInventory(Character ch, Item prefab)
        {
            try
            {
                Player p = ch.player;
                if (p == null) return false;
                ItemSlot slot;
                return p.AddItem(prefab.itemID, null, out slot) && slot != null;
            }
            catch { return false; }
        }

        private static bool SpawnOnGround(Character ch, Item prefab, int index)
        {
            try
            {
                if (!PhotonNetwork.InRoom) return false;
                Vector3 right = ch.transform.right;
                Vector3 pos = ch.transform.position + ch.transform.forward * 1.3f
                              + Vector3.up * 0.4f + right * ((index % 3 - 1) * 0.5f);
                PhotonNetwork.Instantiate("0_Items/" + prefab.gameObject.name, pos,
                    UnityEngine.Random.rotation, 0);
                return true;
            }
            catch { return false; }
        }
    }
}
