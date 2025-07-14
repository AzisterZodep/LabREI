using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;

[System.Serializable]
public class Peca {
    public int id;
    public string nome;
    public string tipo;
    public string estado;
    public int origem;
    public string descricao;
    public string imagem_path;
}

[System.Serializable]
public class ListaPecasWrapper {
    public List<Peca> pecas;
}

[System.Serializable]
public class PC {
    public int id;
    public string nome;
    public string tipo;// adicionei mude o codigo e servidor
    public string estado;// adicionei mude o codigo e servidor
    public string origem;
    public string descricao;
    public List<Peca> pecas;
}

[System.Serializable]
public class ListaPCWrapper { 
    public List<PC> computadores;
}

public class Lista : MonoBehaviour {
    public Transform content;
    public Button btnAdicionar, btnRemover, btnSalvar;
    public Dropdown dropdownFiltro;
    public InputField inputPesquisa;
    public ScrollRect scrollRect;
    public Font fonte;

    public InputField inputNome;
    public InputField inputTipo;
    public InputField inputEstado;
    public InputField inputDescricao;
    public InputField inputOrigem;
    public GameObject imgConfirm;
    public GridContentConect gridConect;
    public List<Peca> todasPecas = new();
    public Peca pecaSelecionada = null;

    public List<PC> todosPC = new();
    public PC PCSelecionado = null;

    public bool desm; 
    public Title msg;

    private Image selectedItemImage;
    public Color defaultItemColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color selectedItemColor = new Color(0.3f, 0.7f, 1f, 1f);
    public Color buttonColor = new Color(0f, 0.66f, 0f, 1f);

    public void modeTipe(bool b) {
        desm = b;
    }
    public void starter() {
        btnRemover.interactable = false;
        btnSalvar.interactable = false;

        SetInputFieldsInteractable(false);
        btnAdicionar.onClick.RemoveAllListeners();
        btnAdicionar.onClick.AddListener(AdicionarItem);
        btnRemover.onClick.RemoveAllListeners();
        btnRemover.onClick.AddListener(RemoverSelecionada);
        btnSalvar.onClick.RemoveAllListeners();
        btnSalvar.onClick.AddListener(SalvarAlteracoes);
        dropdownFiltro.onValueChanged.RemoveAllListeners();
        dropdownFiltro.onValueChanged.AddListener(delegate { AtualizarListaVisual(); });
        inputPesquisa.onValueChanged.RemoveAllListeners();
        inputPesquisa.onValueChanged.AddListener(delegate { AtualizarListaVisual(); });

        if (desm) {
            StartCoroutine(CarregarPC());
            StartCoroutine(CarregarPecas());
        } else {
            StartCoroutine(CarregarPC());
        }
    }

    void SetInputFieldsInteractable(bool interactable) {
        if (inputNome != null) inputNome.interactable = interactable;
        if (inputTipo != null) inputTipo.interactable = interactable;
        if (inputDescricao != null) inputDescricao.interactable = interactable;
        if (inputEstado != null) inputEstado.interactable = interactable;
        if (inputOrigem != null) inputOrigem.interactable = interactable;
    }

    IEnumerator CarregarPecas() {
        msg.Reset();
        using UnityWebRequest www = UnityWebRequest.Get("http://127.0.0.1:5000/status");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            msg.Set("Sem conexão com o servidor: " + www.error);
            msg.SetColor(true);
            yield break;
        }

        using UnityWebRequest req = UnityWebRequest.Get("http://127.0.0.1:5000/pecas");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao carregar peças: " + req.error);
            msg.SetColor(true);
            yield break;
        }

        todasPecas = JsonUtility.FromJson<ListaPecasWrapper>("{\"pecas\":" + req.downloadHandler.text + "}").pecas;
        PopularDropdownTipos();
        AtualizarListaVisual();
    }

    IEnumerator CarregarPC() {
        msg.Reset();
        using UnityWebRequest www = UnityWebRequest.Get("http://127.0.0.1:5000/status");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            msg.Set("Sem de conexão com o servidor: " + www.error);
            msg.SetColor(true);
            yield break;
        }

        using UnityWebRequest req = UnityWebRequest.Get("http://127.0.0.1:5000/computadores");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao carregar PCs: " + req.error);
            msg.SetColor(true);
            yield break;
        }
        
        todosPC = JsonUtility.FromJson<ListaPCWrapper>("{\"computadores\":" + req.downloadHandler.text + "}").computadores;
        PopularDropdownTipos();
        AtualizarListaVisual();
    }

    void PopularDropdownTipos() {
        dropdownFiltro.ClearOptions();
        List<string> estado = new List<string>();
        if (desm) {
            estado = todasPecas.Select(p => p.estado).Distinct().ToList();
        } else {
            estado = todosPC.Select(pc => pc.estado).Distinct().ToList();
        }
        estado.Insert(0, "Todos");
        dropdownFiltro.AddOptions(estado);
    }

    void AtualizarListaVisual() {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        string filtro = inputPesquisa.text.ToLower();
        string tipoSelecionado = dropdownFiltro.options[dropdownFiltro.value].text;

        if (desm) {
            IEnumerable<Peca> listaFiltrada = todasPecas.Where(p =>
                (tipoSelecionado == "Todos" || p.estado == tipoSelecionado) &&
                (p.nome.ToLower().Contains(filtro) || p.descricao.ToLower().Contains(filtro))
            );
            foreach (var peca in listaFiltrada) {
                CriarItemVisual(peca);
            }
        } else {
            IEnumerable<PC> listaFiltrada = todosPC.Where(pc =>
                (tipoSelecionado == "Todos" || pc.estado == tipoSelecionado) &&
                (pc.nome.ToLower().Contains(filtro) || pc.descricao.ToLower().Contains(filtro))
            );
            foreach (var pc in listaFiltrada) {
                CriarItemVisual(pc);
            }
        }
        gridConect.AjustarTamanhoContent();
    }

    // Overload for Peca
    void CriarItemVisual(Peca peca) {
        GameObject item = new GameObject("Item_Peca_" + peca.id);
        item.transform.SetParent(content, false);
        item.AddComponent<VerticalLayoutGroup>();

        Image itemBg = item.AddComponent<Image>();
        itemBg.color = defaultItemColor;

        CriarTexto(item.transform, $"ID: {peca.id}");
        CriarTexto(item.transform, $"{peca.nome}, {peca.tipo}, {peca.estado}");

        Button btn = CriarBotao(item.transform, "Selecionar", () => {
            if (selectedItemImage != null) {
                selectedItemImage.color = defaultItemColor;
            }
            itemBg.color = selectedItemColor;
            selectedItemImage = itemBg;
            pecaSelecionada = peca;
            PCSelecionado = null;
            ExibirDetalhesPeca(peca);
            btnRemover.interactable = true;
            btnSalvar.interactable = true;
            SetInputFieldsInteractable(true);
        });
    }

    void CriarItemVisual(PC pc) {
        GameObject item = new GameObject("Item_PC_" + pc.id);
        item.transform.SetParent(content, false);
        item.AddComponent<VerticalLayoutGroup>();

        Image itemBg = item.AddComponent<Image>();
        itemBg.color = defaultItemColor;

        CriarTexto(item.transform, $"ID: {pc.id}");
        CriarTexto(item.transform, $"{pc.nome}, {pc.tipo}, {pc.origem}.");

        Button btn = CriarBotao(item.transform, "Selecionar", () => {
            if (selectedItemImage != null) {
                selectedItemImage.color = defaultItemColor;
            }
            itemBg.color = selectedItemColor;
            selectedItemImage = itemBg;
            PCSelecionado = pc;
            pecaSelecionada = null;
            ExibirDetalhesPC(pc);
            btnRemover.interactable = true;
            btnSalvar.interactable = true;
            SetInputFieldsInteractable(true);
        });
    }

    void ExibirDetalhesPeca(Peca peca) {
        if (inputNome != null) inputNome.text = peca.nome ?? "";
        if (inputTipo != null) inputTipo.text = peca.tipo ?? "";
        if (inputEstado != null) inputEstado.text = peca.estado ?? "";
        if (inputDescricao != null) inputDescricao.text = peca.descricao ?? "";
        if (inputOrigem != null) {
            PC origemPC = todosPC.FirstOrDefault(pc => pc.id == peca.origem);
            inputOrigem.text = origemPC != null ? origemPC.nome : "N/A"; // Display name, or "N/A" if not found
        }
        imgConfirm.SetActive(false);
    }

    void ExibirDetalhesPC(PC pc) {
        if (inputNome != null) inputNome.text = pc.nome ?? "";
        if (inputTipo != null) inputTipo.text = pc.tipo ?? "";
        if (inputEstado != null) inputEstado.text = pc.estado ?? "";
        if (inputOrigem != null) inputOrigem.text = pc.origem ?? "";
        if (inputDescricao != null) inputDescricao.text = pc.descricao ?? "";
        imgConfirm.SetActive(false);
    }


    Text CriarTexto(Transform parent, string conteudo) {
        GameObject go = new GameObject("Texto");
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.text = conteudo;
        txt.font = fonte;
        txt.fontSize = 14;
        txt.color = Color.black;
        return txt;
    }

    InputField CriarInputField(Transform parent, string placeholder, string initialText = "") {
        GameObject go = new GameObject("InputField");
        go.transform.SetParent(parent, false);
        
        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;

        InputField inputF = go.AddComponent<InputField>();
        inputF.targetGraphic = bg;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        Text txt = textGO.AddComponent<Text>();
        txt.font = fonte;
        txt.fontSize = 14;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleLeft;
        RectTransform txtRect = txt.rectTransform;
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(5, 5);
        txtRect.offsetMax = new Vector2(-5, -5);

        inputF.textComponent = txt;

        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        Text placeholderTxt = placeholderGO.AddComponent<Text>();
        placeholderTxt.font = fonte;
        placeholderTxt.fontSize = 14;
        placeholderTxt.color = Color.grey;
        placeholderTxt.alignment = TextAnchor.MiddleLeft;
        RectTransform placeholderRect = placeholderTxt.rectTransform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(5, 5);
        placeholderRect.offsetMax = new Vector2(-5, -5);
        placeholderTxt.text = placeholder;

        inputF.placeholder = placeholderTxt;
        inputF.text = initialText;

        RectTransform inputRect = go.GetComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(250, 30);

        return inputF;
    }


    Button CriarBotao(Transform parent, string texto, UnityEngine.Events.UnityAction acao) {
        GameObject go = new GameObject("Botao");
        go.transform.SetParent(parent, false);
        Button btn = go.AddComponent<Button>();
        Image img = go.AddComponent<Image>();
        img.color = buttonColor;

        GameObject txtObj = new GameObject("TextoBotao");
        txtObj.transform.SetParent(go.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = texto;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = fonte;
        txt.color = Color.white;
        txt.rectTransform.sizeDelta = new Vector2(160, 30);

        btn.onClick.AddListener(acao);
        return btn;
    }

    void AdicionarItem() {
        if (desm) {
            StartCoroutine(EnviarNovaPecaAoServidor());
        } else {
            StartCoroutine(EnviarNovoPCAoServidor());
        }
    }

    IEnumerator EnviarNovaPecaAoServidor() {
        Peca nova = new Peca {
            nome = "Sem nome",
            tipo = "A definir",
            estado = "A definir",
            origem = -1,
            descricao = "Recém-adicionado",
            imagem_path = ""
        };

        string json = JsonUtility.ToJson(nova);
        using UnityWebRequest req = new UnityWebRequest("http://127.0.0.1:5000/peca", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao adicionar Peca: " + req.error);
            msg.SetColor(true);
        } else {
            StartCoroutine(CarregarPecas());
            SetInputFieldsInteractable(false);
        }
    }

    IEnumerator EnviarNovoPCAoServidor() {
        
        PC novoPC = new PC {
            nome = "Sem nome",
            tipo = "A definir",
            estado = "A definir",
            origem = "A definir",
            pecas = new List<Peca>(),
            descricao = "Recém-adicionado"
        };

        string json = JsonUtility.ToJson(novoPC);
        using UnityWebRequest req = new UnityWebRequest("http://127.0.0.1:5000/computador", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao adicionar computador: " + req.error);
            msg.SetColor(true);
        } else {
            StartCoroutine(CarregarPC());
            SetInputFieldsInteractable(false);
        }
    }

    void RemoverSelecionada() {
        if(desm && pecaSelecionada != null){
            StartCoroutine(DeletarPecaDoServidor(pecaSelecionada.id));
        } else if(PCSelecionado != null){
                StartCoroutine(DeletarPCDoServidor(PCSelecionado.id));
        } else {
            msg.Set("Nenhuma peça ou computador selecionado para remover.");
            msg.SetColor(false);
        }
    }

    IEnumerator DeletarPecaDoServidor(int id) {
        using UnityWebRequest req = UnityWebRequest.Delete("http://127.0.0.1:5000/peca/" + id);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao remover peça: " + req.error);
            msg.SetColor(true);
        } else {
            pecaSelecionada = null;
            btnRemover.interactable = false;
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            StartCoroutine(CarregarPecas());
        }
    }

    IEnumerator DeletarPCDoServidor(int id) {
        using UnityWebRequest req = UnityWebRequest.Delete("http://127.0.0.1:5000/computador/" + id);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao remover computador: " + req.error);
            msg.SetColor(true);
        } else {
            PCSelecionado = null;
            btnRemover.interactable = false;
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            StartCoroutine(CarregarPC());
        }
    }

    void SalvarAlteracoes() {
        if(desm && pecaSelecionada != null){
            StartCoroutine(AtualizarPecaNoServidor());
        } else if(PCSelecionado != null){
            StartCoroutine(AtualizarPCNoServidor());
        } else {
            msg.Set("Nenhuma peça ou computador selecionado para salvar.");
            msg.SetColor(false);
        }
    }

    IEnumerator AtualizarPecaNoServidor() {
        pecaSelecionada.nome = inputNome.text;
        pecaSelecionada.tipo = inputTipo.text;
        pecaSelecionada.estado = inputEstado.text;
        pecaSelecionada.descricao = inputDescricao.text;
        if (int.TryParse(inputOrigem.text, out int parsedId)){
            pecaSelecionada.origem = parsedId;
        } else {
            PC origemPC = todosPC.FirstOrDefault(pc => pc.nome.Equals(inputOrigem.text, System.StringComparison.OrdinalIgnoreCase));
            if (origemPC != null){pecaSelecionada.origem = origemPC.id;} else
            {
            msg.Set($"PC com nome '{inputOrigem.text}' não encontrado. Origem contunuará indefinida.");
            msg.SetColor(false);
            pecaSelecionada.origem = -1;}
        }
        string json = JsonUtility.ToJson(pecaSelecionada);
        using UnityWebRequest req = UnityWebRequest.Put("http://127.0.0.1:5000/peca/" + pecaSelecionada.id, json);
        req.SetRequestHeader("Content-Type", "application/json");
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao atualizar peça: " + req.error);
            msg.SetColor(true);
        } else {
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            PopularDropdownTipos();
            AtualizarListaVisual();
        }
    }

    IEnumerator AtualizarPCNoServidor() {
        PCSelecionado.nome = inputNome.text;
        PCSelecionado.tipo = inputTipo.text;
        PCSelecionado.estado = inputEstado.text;
        PCSelecionado.origem = inputOrigem.text;
        PCSelecionado.descricao = inputDescricao.text;

        string json = JsonUtility.ToJson(PCSelecionado);
        using UnityWebRequest req = UnityWebRequest.Put("http://localhost:5000/computador/" + PCSelecionado.id, json);
        req.SetRequestHeader("Content-Type", "application/json");
        req.downloadHandler = new DownloadHandlerBuffer();

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Inprevisto ao atualizar computador: " + req.error);
            msg.SetColor(true);
        } else {
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            PopularDropdownTipos();
            AtualizarListaVisual();
        }
    }
}