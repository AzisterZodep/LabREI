using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events; 
using System;
using System.Collections.Generic;
using System.Reflection;

[Serializable]
public class ButtonData
{
    public string buttonText = "New Tile";
    public Sprite buttonIcon = null; 
    public Color buttonTextColor = Color.white; 
    public FontStyle style;

    [Header("Button Colors")]
    public Color normalColor = new Color(0.25f, 0.25f, 0.25f, 1f); // Equivalente ao var(--color1) ou similar
    public Color highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    public bool clickable;
    public UnityEvent functions;
    public string functionName = "";
}

public class ButtonGridGenerator : MonoBehaviour
{
    private Vector2 lastScreenSize;
    // Referências no Editor
    public RectTransform panelGrid; // O 'panel' do seu CSS
    public Font buttonFont; // Fonte para o texto dos botões
    public CanvasScaler canvasScaler; // Para obter a resolução de referência do Canvas
    // Variáveis de Estilo para replicar o CSS
    [Header("Panel Style (CSS .panel)")]
    [Range(0, 100)] public float panelWidthVW = 74.5f; // % da largura da tela
    [Range(0, 100)] public float panelMaxHeightVH = 80f; // % da altura da tela
    [Range(0, 10)] public float panelPaddingVH = 3f; // % da altura da tela

    [Header("Tile Style (CSS .tile)")]
    [Range(0, 100)] public float tileWidthVW = 11.5f; // % da largura da tela
    [Range(0, 100)] public float tileHeightVH = 9.5f; // % da largura da tela (sim, height também em VW para consistência com o CSS)
    [Range(0, 10)] public float gapVW = 0.5f; // Espaçamento entre tiles em % da largura da tela
    [Range(0, 10)] public float borderWidthVW = 0.3f; // Largura da borda em % da largura da tela
    [Range(0, 100)] public float fontSizeVW = 2.5f; // Tamanho da fonte em % da largura da tela

    [Header("Base Colors")]
    public Color color1 = new Color(0.1f, 0.1f, 0.1f, 1f); 
    public Color color2 = new Color(0.05f, 0.05f, 0.05f, 1f); // Exemplo de cor mais escura para o gradiente
    public Color color3 = Color.white; // Exemplo de cor clara para texto

    [Header("Button Configurations")]
    public List<ButtonData> buttonConfigs = new List<ButtonData>();

    // Variáveis internas para cálculos
    private float currentViewportWidth;
    private float currentViewportHeight;
    public UnityEvent comumfunctions;

    void Awake()
    {
        // Certifica-se de que temos um CanvasScaler e RectTransform
        if (panelGrid == null) panelGrid = GetComponent<RectTransform>();
        if (canvasScaler == null) canvasScaler = GetComponentInParent<CanvasScaler>();

        if (canvasScaler == null)
        {
            Debug.LogError("CanvasScaler não encontrado no Canvas pai. Certifique-se de que o Canvas tem um CanvasScaler.");
            enabled = false; // Desativa o script se não puder escalar corretamente
            return;
        }

        // Adiciona um Image ao panelGrid se não tiver, para o gradiente
        if (panelGrid.GetComponent<Image>() == null)
        {
            panelGrid.gameObject.AddComponent<Image>();
        }

        // Adiciona um Grid Layout Group se não tiver
        if (panelGrid.GetComponent<GridLayoutGroup>() == null)
        {
            panelGrid.gameObject.AddComponent<GridLayoutGroup>();
        }
    }

    void Start()
    {
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        GenerateButtons(); // Gera os botões
        UpdateButtonSizes(); // Ajusta os tamanhos
    }

    void OnRectTransformDimensionsChange()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            CalculateViewportSizes();
            ApplyPanelStyle(); 
            UpdateButtonSizes(); 
        }
    }

    private void UpdateButtonSizes()
    {
        float borderSize = currentViewportWidth * (borderWidthVW / 100f);
        float calculatedFontSize = currentViewportWidth * (fontSizeVW / 100f);

        foreach (Transform tile in panelGrid)
        {
            // Atualiza tamanho da borda
            Outline outline = tile.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectDistance = new Vector2(borderSize, borderSize);
            }

            // Atualiza tamanho do texto
            Text text = tile.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = Mathf.RoundToInt(calculatedFontSize);
            }
        }
    }
    // Chamado no Editor ou em runtime para regenerar os botões
    [ContextMenu("Generate Buttons")]
    public void GenerateButtons()
    {
        // Limpa botões existentes
        foreach (Transform child in panelGrid)
        {
            Destroy(child.gameObject);
        }

        CalculateViewportSizes();
        ApplyPanelStyle();
        CreateButtonsInGrid();
    }

    // Calcula a largura e altura da viewport com base na resolução de referência do CanvasScaler
    private void CalculateViewportSizes()
    {
        // Se o CanvasScaler estiver usando Scale With Screen Size e Match WidthOrHeight
        // Usamos a resolução de referência para os cálculos de vw/vh
        if (canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            currentViewportWidth = canvasScaler.referenceResolution.x;
            currentViewportHeight = canvasScaler.referenceResolution.y;
        }
        else
        {
            // Fallback para o tamanho real da tela se não estiver em ScaleWithScreenSize
            currentViewportWidth = Screen.width;
            currentViewportHeight = Screen.height;
        }
    }

    // Aplica os estilos do '.panel' CSS ao 'panelGrid'
    private void ApplyPanelStyle()
    {
        // Configura o RectTransform do panel
        panelGrid.sizeDelta = new Vector2(currentViewportWidth * (panelWidthVW / 100f), panelGrid.sizeDelta.y); // Largura baseada em VW

        // Configura o padding do Grid Layout Group
        GridLayoutGroup gridLayout = panelGrid.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            float paddingValue = currentViewportHeight * (panelPaddingVH / 100f);
            gridLayout.padding.left = Mathf.RoundToInt(paddingValue);
            gridLayout.padding.right = Mathf.RoundToInt(paddingValue);
            gridLayout.padding.top = Mathf.RoundToInt(paddingValue);
            gridLayout.padding.bottom = Mathf.RoundToInt(paddingValue);

            // Configura o espaçamento (gap)
            float gapValue = currentViewportWidth * (gapVW / 100f);
            float gapValue1 = currentViewportHeight * (gapVW / 100f);
            gridLayout.spacing = new Vector2(gapValue, gapValue1); // Gap em VW

            // Configura o tamanho da célula (tile)
            float tileWidth = currentViewportWidth * (tileWidthVW / 100f);
            float tileHeight = currentViewportHeight * (tileHeightVH / 100f); // Altura do tile também baseada em VW, como no seu CSS
            gridLayout.cellSize = new Vector2(tileWidth, tileHeight);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        // Configura o background do panel (linear-gradient)
        Image panelImage = panelGrid.GetComponent<Image>();
        if (panelImage != null)
        {
            // Para gradiente, precisamos de um material com shader de gradiente
            // ou usar uma imagem gerada programaticamente.
            // Para simplicidade, vamos usar uma cor sólida ou uma textura de gradiente pré-feita.
            // Se você quiser um gradiente exato, precisaria criar uma textura ou um shader.
            // Por enquanto, vamos usar a color1 como cor base.
            // panelImage.color = color1; // Ou atribuir uma textura de gradiente
            
            // Uma forma simples de simular o gradiente é com uma imagem de gradiente no Asset.
            // Para criar um gradiente 180deg com var(--color1) e var(--color2) programaticamente:
            Texture2D gradientTex = new Texture2D(1, 2); // Textura de 1px de largura por 2px de altura
            gradientTex.SetPixel(0, 0, color1); // Cor superior
            gradientTex.SetPixel(0, 1, color2); // Cor inferior
            gradientTex.Apply();
            Sprite gradientSprite = Sprite.Create(gradientTex, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f));
            panelImage.sprite = gradientSprite;
            panelImage.type = Image.Type.Sliced; // Para melhor ajuste
            panelImage.color = Color.white; // Manter branco para não tingir o gradiente
        }

        // Configura max-height e overflow
        // Max-height é configurado no RectTransform
        panelGrid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentViewportHeight * (panelMaxHeightVH / 100f));
    }

    // Cria os botões (tiles) dinamicamente
    private void CreateButtonsInGrid()
    {
        float borderSize = currentViewportWidth * (borderWidthVW / 100f);
        float calculatedFontSize = currentViewportWidth * (fontSizeVW / 100f);

        foreach (ButtonData config in buttonConfigs)
        {
            // 1. Criar o GameObject do botão (tile)
            GameObject newButtonGO = new GameObject("Tile_" + config.buttonText.Replace(" ", ""), typeof(RectTransform));
            newButtonGO.transform.SetParent(panelGrid, false); // Define o pai e mantém a posição local

            // 2. Adicionar o componente Image (para o fundo do tile)
            Image buttonImage = newButtonGO.AddComponent<Image>();
            buttonImage.raycastTarget = true;
            buttonImage.color = config.normalColor; // Cor normal inicial

            // 3. Adicionar o componente Button
            Button newButton = newButtonGO.AddComponent<Button>();

            // Configurar as cores de transição
            ColorBlock colors = newButton.colors;
            colors.normalColor = config.normalColor;
            colors.highlightedColor = config.highlightedColor;
            colors.pressedColor = config.pressedColor;
            colors.selectedColor = config.selectedColor;
            colors.disabledColor = config.disabledColor;
            newButton.colors = colors;
            newButton.transition = Selectable.Transition.ColorTint;

            // Configurar a borda (na Unity, isso geralmente é feito com uma segunda imagem ou shader)
            // Para simular a borda, podemos usar um Outline ou uma imagem de borda.
            // Para simplicidade e replicar o border-width, vamos adicionar um Outline aqui.
            Outline buttonOutline = newButtonGO.AddComponent<Outline>();
            buttonOutline.effectColor = new Color(0, 0, 0, 0); // Transparente inicialmente
            buttonOutline.effectDistance = new Vector2(borderSize, borderSize); // Largura da borda

            // 4. Adicionar o componente Text (para o rótulo do tile)
            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(newButtonGO.transform, false);
            Text buttonText = textGO.AddComponent<Text>();
            buttonText.text = config.buttonText;
            buttonText.font = buttonFont != null ? buttonFont : Font.CreateDynamicFontFromOSFont("Arial", Mathf.RoundToInt(calculatedFontSize * 2)); // Font size em pixels
            buttonText.fontSize = Mathf.RoundToInt(calculatedFontSize);
            buttonText.color = config.buttonTextColor;
            buttonText.alignment = TextAnchor.LowerCenter;
            buttonText.fontStyle = config.style;
            buttonText.resizeTextForBestFit = true;
            buttonText.resizeTextMinSize = 1;
            buttonText.resizeTextMaxSize = 40;

            // Ajustar o RectTransform do texto para preencher a parte inferior do botão
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.25f); // Ocupa os 25% inferiores do tile
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = new Vector2(0, 0);

            // 5. Opcional: Adicionar ícone ao tile (se houver um sprite)
            if (config.buttonIcon != null)
            {
                GameObject iconGO = new GameObject("Icon", typeof(RectTransform));
                iconGO.transform.SetParent(newButtonGO.transform, false);
                Image iconImage = iconGO.AddComponent<Image>();
                iconImage.sprite = config.buttonIcon;
                iconImage.preserveAspect = true;

                // Posicionar o ícone acima do texto
                RectTransform iconRect = iconGO.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.25f); // Acima do texto
                iconRect.anchorMax = new Vector2(1f, 1f); // Ocupa a parte superior restante
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(0, 0); // Preenche o espaço disponível
            }
            if(config.clickable){
            //btn_conclude.onClick.RemoveAllListeners();
            // 6. Configurar a função onClick (usando Reflection)
            if (!string.IsNullOrEmpty(config.functionName))
            {
                MethodInfo method = GetType().GetMethod(config.functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    newButton.onClick.AddListener(() => method.Invoke(this, null));
                }
                else
                {
                    Debug.LogWarning($"Função '{config.functionName}' não encontrada em '{gameObject.name}'. Verifique o nome e a visibilidade (public/private).");
                }
            }
            // Add listeners from the common functions array
            if (comumfunctions != null)
            {
                newButton.onClick.AddListener(() => comumfunctions.Invoke());
            }
            // Add listeners from the button-specific functions array
            if (config.functions != null)
            {
                newButton.onClick.AddListener(() => config.functions.Invoke());
            }
            }
        }
    }
    // --- EXEMPLOS DE FUNÇÕES PARA OS BOTÕES ---
    // Você pode adicionar suas próprias funções aqui.
    // Elas precisam ser acessíveis (public ou com BindingFlags.NonPublic | BindingFlags.Instance)
    // para serem chamadas via Reflection.
}