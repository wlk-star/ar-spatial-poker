using System.Collections.Generic;
using UnityEngine;

namespace SpatialPoker.Presentation.Cards
{
    /// <summary>
    /// Loads procedural card-face textures from Resources/CardFaces.
    /// Codes match the server format: rank of 23456789TJQKA + suit of cdhs
    /// (e.g. "Ah", "Td", "7s"). Textures live at
    /// Assets/SpatialPoker/Textures/Cards/Resources/CardFaces/card_front_*.png.
    /// </summary>
    public static class CardFaceLibrary
    {
        private const string FacesRoot = "CardFaces";
        private const string BackName = "card_back";

        private static readonly Dictionary<string, Texture2D> FaceCache =
            new Dictionary<string, Texture2D>();

        private static readonly Dictionary<Renderer, Material> SlotMaterials =
            new Dictionary<Renderer, Material>();

        private static Texture2D back;

        public static Texture2D GetFace(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2)
                return null;
            var normalized = code.Trim();
            if (normalized.Length > 2)
                normalized = normalized.Substring(0, 2);

            if (FaceCache.TryGetValue(normalized, out var cached))
                return cached;

            var tex = Resources.Load<Texture2D>($"{FacesRoot}/card_front_{normalized}");
            if (tex != null)
            {
                FaceCache[normalized] = tex;
            }
            else
            {
                Debug.LogWarning($"[SpatialPoker] Missing card face texture for '{normalized}'.");
            }
            return tex;
        }

        public static Texture2D GetBack()
        {
            if (back == null)
                back = Resources.Load<Texture2D>($"{FacesRoot}/{BackName}");
            return back;
        }

        /// <summary>
        /// Applies a card code to a slot: shows/hides the slot, swaps the face
        /// texture on a per-slot material instance, and falls back to the text
        /// label when the texture is missing.
        /// </summary>
        public static void ApplyToSlot(
            GameObject slot,
            Renderer slotRenderer,
            Material faceTemplate,
            TMPro.TMP_Text label,
            string code)
        {
            if (slot == null || slotRenderer == null)
                return;

            var hasCard = !string.IsNullOrEmpty(code);
            slot.SetActive(hasCard);
            if (!hasCard)
                return;

            if (!SlotMaterials.TryGetValue(slotRenderer, out var mat) || mat == null)
            {
                mat = faceTemplate != null
                    ? new Material(faceTemplate)
                    : new Material(Shader.Find("Standard"));
                slotRenderer.material = mat;
                SlotMaterials[slotRenderer] = mat;
            }

            var face = GetFace(code);
            mat.mainTexture = face;

            if (label != null)
                label.text = face != null ? string.Empty : code;
        }
    }
}
