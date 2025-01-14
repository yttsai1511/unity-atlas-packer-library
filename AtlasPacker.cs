using System.Collections.Generic;

namespace UnityEngine.U2D
{
    public static class AtlasPacker
    {
        public struct BinNode
        {
            private RectInt _spaceRect;
            private BinNode[] _childNodes;

            public bool IsValid { get;  private set; }

            public BinNode(int x, int y, int width, int height)
            {
                _spaceRect = new RectInt(x, y, width, height);
                _childNodes = null;
                IsValid = true;
            }

            public bool TryPlace(Texture2D texture, int padding, out RectInt texRect)
            {
                int spaceWidth = texture.width + padding;
                int spaceHeight = texture.height + padding;
                bool isPortrait = spaceHeight >= spaceWidth;

                if (!FindSpace(spaceWidth, spaceHeight, isPortrait, out var spaceRect))
                {
                    texRect = default;
                    return false;
                }

                SplitSpace(spaceRect, isPortrait);

                int offset = padding / 2;
                texRect = new RectInt(spaceRect.xMin + offset, spaceRect.yMin + offset, texture.width, texture.height);
                return true;
            }

            public bool FindSpace(int width, int height, bool isPortrait, out RectInt spaceRect)
            {
                spaceRect = default;

                if (_childNodes != null)
                {
                    foreach (var node in _childNodes)
                    {
                        if (!node.IsValid)
                        {
                            continue;
                        }

                        if (node.FindSpace(width, height, isPortrait, out spaceRect))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                if (width > _spaceRect.width || height > _spaceRect.height)
                {
                    return false;
                }

                if (isPortrait)
                {
                    spaceRect = new RectInt(_spaceRect.xMin, _spaceRect.yMin, width, height);
                    return true;
                }
                else
                {
                    spaceRect = new RectInt(_spaceRect.xMax - width, _spaceRect.yMax - height, width, height);
                    return true;
                }
            }

            public bool SplitSpace(RectInt target, bool isPortrait)
            {
                if (!_spaceRect.Overlaps(target))
                {
                    return false;
                }

                bool isSplit = false;

                if (_childNodes != null)
                {
                    foreach (var node in _childNodes)
                    {
                        if (node.IsValid)
                        {
                            isSplit |= node.SplitSpace(target, isPortrait);
                        }
                    }
                    return isSplit;
                }

                _childNodes = new BinNode[2];

                if (isPortrait)
                {
                    int width = _spaceRect.xMax - target.xMax;
                    if (width > 0f)
                    {
                        var right = new BinNode(target.xMax, _spaceRect.yMin, width, _spaceRect.height);
                        _childNodes[0] = right;
                        isSplit = true;
                    }

                    int height = _spaceRect.yMax - target.yMax;
                    if (height > 0f)
                    {
                        var top = new BinNode(_spaceRect.xMin, target.yMax, _spaceRect.width, height);
                        _childNodes[1] = top;
                        isSplit = true;
                    }
                }
                else
                {
                    int width = target.xMin - _spaceRect.xMin;
                    if (width > 0f)
                    {
                        var left = new BinNode(_spaceRect.xMin, _spaceRect.yMin, width, _spaceRect.height);
                        _childNodes[0] = left;
                        isSplit = true;
                    }

                    int height = target.yMin - _spaceRect.yMin;
                    if (height > 0f)
                    {
                        var down = new BinNode(_spaceRect.xMin, _spaceRect.yMin, _spaceRect.width, height);
                        _childNodes[1] = down;
                        isSplit = true;
                    }
                }
                return isSplit;
            }

            public void Clear()
            {
                if (_childNodes != null)
                {
                    foreach (var node in _childNodes)
                    {
                        node.Clear();
                    }
                    _childNodes = null;
                }
            }
        }

        /// <summary>
        /// Sorts textures based on their maximum dimension in descending order.
        /// </summary>
        /// <param name="source">The collection of textures to sort.</param>
        /// <param name="maxSize">The maximum allowable size for textures.</param>
        /// <returns>A sorted list of textures that fit within the maximum size.</returns>
        public static List<Texture2D> SortTextures(IEnumerable<Texture2D> source, int maxSize)
        {
            var textures = new List<Texture2D>();
            textures.AddRange(source);
            textures.RemoveAll((tex) => tex.width > maxSize || tex.height > maxSize);
            textures.Sort((a, b) => Mathf.Max(b.width, b.height).CompareTo(Mathf.Max(a.width, a.height)));
            return textures; 
        }

        /// <summary>
        /// Packs textures into multiple groups, ensuring each group fits within the specified maximum size.
        /// </summary>
        /// <param name="textures">The list of textures to pack.</param>
        /// <param name="padding">The padding to apply between textures.</param>
        /// <param name="maxSize">The maximum size of each atlas.</param>
        /// <returns>A list of texture arrays, each representing a batch for a single atlas.</returns>
        public static List<Texture2D[]> PackTextures(List<Texture2D> textures, int padding, int maxSize)
        {
            var batch = new List<Texture2D>();
            var root = new BinNode(0, 0, maxSize, maxSize);
            var results = new List<Texture2D[]>();

            while (textures.Count > 0)
            {
                for (int index = 0; index < textures.Count; index++)
                {
                    Texture2D tex = textures[index];
                    if (root.TryPlace(tex, padding, out _))
                    {
                        batch.Add(tex);
                        textures.RemoveAt(index);
                        index--;
                    }
                }
                results.Add(batch.ToArray());

                batch.Clear();
                root.Clear();
            }
            return results;
        }

        /// <summary>
        /// Generates a single atlas texture from the provided textures.
        /// </summary>
        /// <param name="textures">The array of textures to pack.</param>
        /// <param name="padding">The padding to apply between textures in the atlas.</param>
        /// <param name="maxSize">The maximum size of the atlas.</param>
        /// <param name="atlas">The generated atlas texture as output.</param>
        /// <returns>An array of Rect structures representing the UV coordinates of each texture in the atlas.</returns>
        public static Rect[] GenerateAtlas(Texture2D[] textures, int padding, int maxSize, out Texture2D atlas)
        {
            atlas = new Texture2D(2, 2);
            return atlas.PackTextures(textures, padding, maxSize);
        }

        /// <summary>
        /// Remaps a sprite to a newly generated atlas texture.
        /// </summary>
        /// <param name="source">The original sprite to remap.</param>
        /// <param name="atlas">The atlas texture containing the sprite.</param>
        /// <param name="ratioRect">The UV coordinates of the sprite within the atlas, in 0-1 ratio.</param>
        /// <returns>A new sprite mapped to the atlas texture.</returns>
        public static Sprite RemapSprite(Sprite source, Texture2D atlas, Rect ratioRect)
        {
            var pixelRect = new Rect(ratioRect.x * atlas.width, ratioRect.y * atlas.height, ratioRect.width * atlas.width, ratioRect.height * atlas.height);
            var ratioPivot = new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height);
            return Sprite.Create(atlas, pixelRect, ratioPivot, source.pixelsPerUnit, 0u, SpriteMeshType.Tight, source.border, false);
        }
    }
}