# Unity Atlas Packer Library

一個專為 Unity 執行環境設計的高效能紋理合併工具，解決內建 `Texture2D.PackTextures` 功能在建立多圖集支援不足的問題，提供高效且實用的解決方案。

## 功能特色

- **動態合併**：在執行環境中合併紋理，適用於動態載入的場景。
- **分批處理**：當單一圖集空間不足時，會自動將剩餘的紋理分組處理。
- **高效分配**：基於簡單且快速的二分法，提供穩定的紋理擺放效果。
- **彈性設定**：提供圖集大小與紋理間距的自訂設定，能根據需求調整。

## 使用範例

以下範例展示如何利用 Atlas Packer 處理紋理，並產生多個圖集：

```csharp
var sourceTextures = new Dictionary<Texture2D, List<Image>>();

// 取得 Image 組件及其紋理

List<Texture2D> sortedTextures = AtlasPacker.SortTextures(sourceTextures.Keys, 2048); // 排序

List<Texture2D[]> textureBatches = AtlasPacker.PackTextures(sortedTextures, 8, 2048); // 分組

foreach (var batch in textureBatches)
{
    Rect[] atlasRects = AtlasPacker.GenerateAtlas(batch, 8, 2048, out var atlas); // 合併

    for (int i = 0; i < batch.Length; i++)
    {
        Texture2D texture = batch[i];
        Rect uv = atlasRects[i];

        if (sourceTextures.TryGetValue(texture, out var images))
        {
            foreach (var image in images)
            {
                Sprite sprite = AtlasPacker.RemapSprite(image.sprite, atlas, uv); // 映射
                image.sprite = sprite;
            }
        }
    }
}
```

## 文件說明

| 方法名稱 | 功能描述 |
|:-----|:-----|
| SortTextures | 根據最大邊長對紋理進行排序，並移除超出指定大小的紋理 |
| PackTextures | 將紋理分批處理，回傳多組紋理陣列，適用於產生多個圖集的場景  |
| GenerateAtlas | 使用 Unity 的內建方法生成單一圖集，並回傳對應的 UV 資訊  |
| RemapSprite | 將原始 Sprite 映射到產生的圖集中，並回傳新的 Sprite |

## 授權

此專案基於 GPLv3 授權條款，詳情請參閱 LICENSE 文件。