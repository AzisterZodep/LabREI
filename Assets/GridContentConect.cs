using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class GridContentConect : MonoBehaviour
{
    public RectTransform rectTransform;
    public GridLayoutGroup gridLayout;

    void OnRectTransformDimensionsChange()
    {
        // Detecta automaticamente quando o tamanho do content muda
        AjustarTamanhoContent();
    }

    public void AjustarTamanhoContent()
    {
        int totalItens = rectTransform.childCount;

        // Paddings
        float paddingTop = gridLayout.padding.top;
        float paddingBottom = gridLayout.padding.bottom;
        float paddingLeft = gridLayout.padding.left;
        float paddingRight = gridLayout.padding.right;

        // Largura disponível
        float contentWidth = rectTransform.rect.width - paddingLeft - paddingRight;

        // Quantas colunas cabem?
        int colunas = Mathf.FloorToInt((contentWidth + gridLayout.spacing.x) / (gridLayout.cellSize.x + gridLayout.spacing.x));
        if (colunas < 1) colunas = 1;

        // Quantas linhas são necessárias?
        int linhas = Mathf.CeilToInt((float)totalItens / colunas);

        // Altura total
        float altura = paddingTop + paddingBottom +
                       linhas * gridLayout.cellSize.y +
                       (linhas - 1) * gridLayout.spacing.y;

        // Atualiza o tamanho do Content
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, altura);
    }
}
