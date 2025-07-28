using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Linq;
using System.IO; // Necessário para Path.Combine

// Remova o using System.Text.RegularExpressions; se não estiver usando Regex em outro lugar

[System.Serializable]
public class Peca {
    public int id;
    public string nome;
    public string tipo;
    public string estado;
    public int origem;
    public string descricao;
    public string imagem_path;
    public string filial_origem; // Added field to identify the sister branch
}

[System.Serializable]
public class ListaPecasWrapper {
    public List<Peca> pecas;
}

[System.Serializable]
public class PC {
    public int id;
    public string nome;
    public string tipo;
    public string estado;
    public string origem;
    public string descricao;
    public List<Peca> pecas;
    public string filial_origem; // Added field to identify the sister branch
}

[System.Serializable]
public class ListaPCWrapper {
    public List<PC> computadores;
}

[System.Serializable]
public class LoginRequest {
    public string username;
    public string password;
}

// New class for login response
[System.Serializable]
public class LoginResponse {
    public string message;
    public int user_id;
    public string username;
    public string error;
}

public class Lista : MonoBehaviour {
    // Estas variáveis serão preenchidas ao carregar o arquivo
    public string address;
    public List<string> sisteraddresses = new List<string>();
    public List<string> sisternames = new List<string>();

    // Não precisamos mais de um TextAsset público no Inspector
    // public TextAsset networkConfigTextAsset; // REMOVA OU COMENTE ESTA LINHA

    public Transform content;

    public GameObject loginPanel;
    public InputField loginUsernameInput;
    public InputField loginPasswordInput;
    public Button loginButton;
    public Text loginMessageText;

    public Button btnAdicionar, btnRemover, btnSalvar;
    public Dropdown dropdownFiltro;
    public InputField inputPesquisa;
    public ScrollRect scrollRect;
    public Font fonte;

    private int authenticatedUserId = -1;
    private string authenticatedUsername = "";

    public InputField inputNome;
    public InputField inputTipo;
    public InputField inputEstado;
    public InputField inputDescricao;
    public InputField inputOrigem;
    public GameObject imgConfirm;
    public GridContentConect gridConect; // Assuming this class exists
    public List<Peca> todasPecas = new();
    public Peca pecaSelecionada = null;

    public List<PC> todosPC = new();
    public PC PCSelecionado = null;

    public bool desm;
    public bool sisters; // If true, fetches data from sister servers
    public Title msg; // Assuming this class exists

    private Image selectedItemImage;
    public Color defaultItemColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color selectedItemColor = new Color(0.3f, 0.7f, 1f, 1f);
    public Color buttonColor = new Color(0f, 0.66f, 0f, 1f);

    void Awake() {
        // Inicia a corrotina para carregar a configuração de rede
        StartCoroutine(LoadNetworkConfigFromStreamingAssets());
    }

    public void modeTipe(bool b) {
        desm = b;
    }
    public void localeTipe(bool b) {
        sisters = b;
    }

    public void starter() {
        // Certifique-se de que a configuração de rede foi carregada antes de iniciar as corrotinas de dados
        // Pode ser necessário adicionar uma flag ou esperar a corrotina de LoadNetworkConfigFromStreamingAssets
        // para garantir que 'address' e 'sisteraddresses' estejam preenchidos.
        // Por enquanto, assumimos que Awake() completa antes de starter() ser chamado.
        // Se starter() for chamado muito cedo, adicione um yield return ou uma flag.
        btnRemover.interactable = false;
        btnSalvar.interactable = false;

        SetInputFieldsInteractable(false);
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(AttemptLogin);
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
            StartCoroutine(CarregarPecas());
            StartCoroutine(CarregarPC());
        } else {
            StartCoroutine(CarregarPC());
        }
    }

    IEnumerator LoadNetworkConfigFromStreamingAssets() {
        string filePath = Path.Combine(Application.streamingAssetsPath, "NetworkConfig.txt");
        Debug.Log($"Attempting to load NetworkConfig from: {filePath}");

        UnityWebRequest www = UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError) {
            Debug.LogError($"Error loading NetworkConfig from StreamingAssets: {www.error}");
            // Handle error, e.g., show an error message to the user
        } else {
            string configContent = www.downloadHandler.text;
            string[] lines = configContent.Split('\n');

            sisteraddresses.Clear(); // Limpa listas para evitar duplicação em múltiplos loads
            sisternames.Clear();

            foreach (string line in lines) {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                string[] parts = trimmedLine.Split(':');

                if (parts.Length >= 2) {
                    if (parts[0] == "main_address") {
                        if (parts.Length >= 3) {
                             address = $"{parts[1]}:{parts[2]}"; // IP:Port
                             Debug.Log($"Main Address Loaded: {address}");
                        } else {
                            Debug.LogWarning($"Invalid format for main_address in NetworkConfig.txt: {trimmedLine}");
                        }
                    } else if (parts[0] == "sister_branch" && parts.Length >= 4) { // Name:IP:Port
                        string sisterName = parts[1];
                        string sisterIpPort = $"{parts[2]}:{parts[3]}"; // IP:Port
                        sisternames.Add(sisterName);
                        sisteraddresses.Add(sisterIpPort);
                        Debug.Log($"Sister Branch Loaded: {sisterName} at {sisterIpPort}");
                    } else {
                        Debug.LogWarning($"Unrecognized or invalid line in NetworkConfig.txt: {trimmedLine}");
                    }
                }
            }

            if (string.IsNullOrEmpty(address)) {
                Debug.LogError("Main address not found in NetworkConfig.txt! Please ensure it's formatted correctly.");
            }
        }
        starter();
    }

    public void AttemptLogin() {
        if (string.IsNullOrEmpty(loginUsernameInput.text) || string.IsNullOrEmpty(loginPasswordInput.text)) {
            loginMessageText.text = "Por favor, insira usuário e senha.";
            loginMessageText.color = Color.red;
            return;
        }
        StartCoroutine(SendLoginRequest(loginUsernameInput.text, loginPasswordInput.text));
    }

    IEnumerator SendLoginRequest(string username, string password) {
        // Certifique-se de que 'address' foi carregado antes de usar
        if (string.IsNullOrEmpty(address)) {
            loginMessageText.text = "Erro: Endereço do servidor principal não carregado. Tente novamente.";
            loginMessageText.color = Color.red;
            yield break; // Sai da corrotina se o endereço não estiver pronto
        }

        LoginRequest loginData = new LoginRequest { username = username, password = password };
        string json = JsonUtility.ToJson(loginData);

        using UnityWebRequest req = new UnityWebRequest($"http://{address}/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        loginMessageText.text = "Tentando login...";
        loginMessageText.color = Color.black;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"Login failed: {req.error}");
            LoginResponse errorResponse = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
            loginMessageText.text = errorResponse?.error ?? "Falha no login. Verifique suas credenciais.";
            loginMessageText.color = Color.red;
        } else {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
            if (!string.IsNullOrEmpty(response.error)) {
                loginMessageText.text = response.error;
                loginMessageText.color = Color.red;
            } else {
                authenticatedUserId = response.user_id;
                authenticatedUsername = response.username;
                loginMessageText.text = $"Login bem-sucedido! Bem-vindo, {authenticatedUsername}.";
                loginMessageText.color = Color.green;
                Debug.Log($"Logged in as User ID: {authenticatedUserId}, Username: {authenticatedUsername}");

                loginPanel.SetActive(false);
            }
        }
    }

    private void AddAuthHeaders(UnityWebRequest request) {
        if (authenticatedUserId != -1 && !string.IsNullOrEmpty(authenticatedUsername)) {
            request.SetRequestHeader("X-User-ID", authenticatedUserId.ToString());
            request.SetRequestHeader("X-Username", authenticatedUsername);
        } else {
            Debug.LogWarning("Tentativa de fazer uma requisição autenticada sem estar logado.");
        }
    }

    void SetInputFieldsInteractable(bool interactable) {
        if (inputNome != null) inputNome.interactable = interactable;
        if (inputTipo != null) inputTipo.interactable = interactable;
        if (inputDescricao != null) inputDescricao.interactable = interactable;
        if (inputEstado != null) inputEstado.interactable = interactable;
        if (inputOrigem != null) inputOrigem.interactable = interactable;
    }

    // --- Data Loading Coroutines ---

    IEnumerator CarregarPecas() {
        // Certifique-se de que 'address' e 'sisteraddresses' foram carregados
        if (string.IsNullOrEmpty(address)) {
            msg.Set("Erro: Endereços do servidor não carregados. Não é possível carregar peças.");
            msg.SetColor(true);
            yield break;
        }

        todasPecas.Clear(); // Clear existing pieces before loading

        // Load from main server
        yield return StartCoroutine(FetchPecasFromUrl($"http://{address}/pecas", "Matriz"));

        // Load from sister servers if 'sisters' is enabled
        if (sisters) {
            for (int i = 0; i < sisteraddresses.Count; i++) {
                string sisterAddress = sisteraddresses[i];
                string sisterName = sisternames[i];
                yield return StartCoroutine(FetchPecasFromUrl($"http://{sisterAddress}/pecas", sisterName));
            }
        }
        PopularDropdownTipos();
        AtualizarListaVisual();
    }

    IEnumerator FetchPecasFromUrl(string url, string filialName) {
        msg.Reset();
        using UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            msg.Set($"Erro ao carregar peças de {filialName}: {www.error}");
            msg.SetColor(true);
        } else {
            ListaPecasWrapper wrapper = JsonUtility.FromJson<ListaPecasWrapper>("{\"pecas\":" + www.downloadHandler.text + "}");
            foreach (var peca in wrapper.pecas) {
                peca.filial_origem = filialName; // Assign the origin branch
                todasPecas.Add(peca);
            }
        }
    }

    IEnumerator CarregarPC() {
        // Certifique-se de que 'address' e 'sisteraddresses' foram carregados
        if (string.IsNullOrEmpty(address)) {
            msg.Set("Erro: Endereços do servidor não carregados. Não é possível carregar PCs.");
            msg.SetColor(true);
            yield break;
        }

        todosPC.Clear(); // Clear existing PCs before loading

        // Load from main server
        yield return StartCoroutine(FetchPCFromUrl($"http://{address}/computadores", "Matriz"));

        // Load from sister servers if 'sisters' is enabled
        if (sisters) {
            for (int i = 0; i < sisteraddresses.Count; i++) {
                string sisterAddress = sisteraddresses[i];
                string sisterName = sisternames[i];
                yield return StartCoroutine(FetchPCFromUrl($"http://{sisterAddress}/computadores", sisterName));
            }
        }
        PopularDropdownTipos();
        AtualizarListaVisual();
    }

    IEnumerator FetchPCFromUrl(string url, string filialName) {
        msg.Reset();
        using UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success) {
            msg.Set($"Erro ao carregar PCs de {filialName}: {www.error}");
            msg.SetColor(true);
        } else {
            ListaPCWrapper wrapper = JsonUtility.FromJson<ListaPCWrapper>("{\"computadores\":" + www.downloadHandler.text + "}");
            foreach (var pc in wrapper.computadores) {
                pc.filial_origem = filialName; // Assign the origin branch
                todosPC.Add(pc);
            }
        }
    }

    void PopularDropdownTipos() {
        dropdownFiltro.ClearOptions();
        List<string> estados = new List<string>();
        if (desm) {
            estados = todasPecas.Select(p => p.estado).Distinct().ToList();
        } else {
            estados = todosPC.Select(pc => pc.estado).Distinct().ToList();
        }
        estados.Insert(0, "Todos");
        dropdownFiltro.AddOptions(estados);
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
        if (gridConect != null) { // Added null check for gridConect
            gridConect.AjustarTamanhoContent();
        }
    }

    void CriarItemVisual(Peca peca) {
        GameObject item = new GameObject($"Item_Peca_{peca.filial_origem}_{peca.id}");
        item.transform.SetParent(content, false);
        item.AddComponent<VerticalLayoutGroup>();

        Image itemBg = item.AddComponent<Image>();
        itemBg.color = defaultItemColor;

        CriarTexto(item.transform, $"ID: {peca.filial_origem}-{peca.id}"); // Display branch name
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
        GameObject item = new GameObject($"Item_PC_{pc.filial_origem}_{pc.id}");
        item.transform.SetParent(content, false);
        item.AddComponent<VerticalLayoutGroup>();

        Image itemBg = item.AddComponent<Image>();
        itemBg.color = defaultItemColor;

        CriarTexto(item.transform, $"ID: {pc.filial_origem}-{pc.id}"); // Display branch name
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
            // Find the PC from either the main server or sister servers
            PC origemPC = todosPC.FirstOrDefault(pc => pc.id == peca.origem && pc.filial_origem == peca.filial_origem);
            inputOrigem.text = origemPC != null ? $"{origemPC.filial_origem}-{origemPC.nome}" : "N/A";
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
        // Always send new items to the main server
        using UnityWebRequest req = new UnityWebRequest($"http://{address}/peca", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeaders(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Imprevisto ao adicionar Peca: " + req.error);
            msg.SetColor(true);
        } else {
            msg.Set("Peça adicionada com sucesso!");
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
        // Always send new items to the main server
        using UnityWebRequest req = new UnityWebRequest($"http://{address}/computador", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AddAuthHeaders(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set("Imprevisto ao adicionar computador: " + req.error);
            msg.SetColor(true);
        } else {
            msg.Set("Computador adicionado com sucesso!");
            StartCoroutine(CarregarPC());
            SetInputFieldsInteractable(false);
        }
    }
    void RemoverSelecionada() {
        if(desm && pecaSelecionada != null){
            // Ensure we delete from the correct origin server
            StartCoroutine(DeletarPecaDoServidor(pecaSelecionada.id, pecaSelecionada.filial_origem));
        } else if(PCSelecionado != null){
            // Ensure we delete from the correct origin server
            StartCoroutine(DeletarPCDoServidor(PCSelecionado.id, PCSelecionado.filial_origem));
        } else {
            msg.Set("Nenhuma peça ou computador selecionado para remover.");
            msg.SetColor(false);
        }
    }
    IEnumerator DeletarPecaDoServidor(int id, string filialOrigem) {
        string targetAddress = (filialOrigem == "Matriz") ? address : sisteraddresses[sisternames.IndexOf(filialOrigem)];
        using UnityWebRequest req = UnityWebRequest.Delete($"http://{targetAddress}/peca/" + id);
        AddAuthHeaders(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set($"Imprevisto ao remover peça de {filialOrigem}: {req.error}");
            msg.SetColor(true);
        } else {
            msg.Set($"Peça ID {id} de {filialOrigem} removida com sucesso!");
            pecaSelecionada = null;
            btnRemover.interactable = false;
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            StartCoroutine(CarregarPecas());
        }
    }
    IEnumerator DeletarPCDoServidor(int id, string filialOrigem) {
        string targetAddress = (filialOrigem == "Matriz") ? address : sisteraddresses[sisternames.IndexOf(filialOrigem)];
        using UnityWebRequest req = UnityWebRequest.Delete($"http://{targetAddress}/computador/" + id);
        AddAuthHeaders(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set($"Imprevisto ao remover computador de {filialOrigem}: {req.error}");
            msg.SetColor(true);
        } else {
            msg.Set($"Computador ID {id} de {filialOrigem} removido com sucesso!");
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

        // Attempt to parse inputOrigem.text as an ID first
        if (int.TryParse(inputOrigem.text, out int parsedId)){
            pecaSelecionada.origem = parsedId;
        } else {
            // If not an ID, try to find a PC by name
            PC origemPC = todosPC.FirstOrDefault(pc => pc.nome.Equals(inputOrigem.text, System.StringComparison.OrdinalIgnoreCase));
            if (origemPC != null){
                pecaSelecionada.origem = origemPC.id;
                // Important: If you allow linking to PCs from sister branches,
                // you might also want to update 'filial_origem' here if the piece is being
                // moved to a PC in a different branch. This logic depends on your
                // backend's handling of inter-branch piece assignments.
                msg.Set($"Origem definida para o PC: {origemPC.filial_origem}-{origemPC.nome}.");
                msg.SetColor(true);
            } else {
                msg.Set($"PC com nome '{inputOrigem.text}' não encontrado. Origem continuará indefinida.");
                msg.SetColor(false);
                pecaSelecionada.origem = -1;
            }
        }

        string json = JsonUtility.ToJson(pecaSelecionada);
        // Send update to the server where the piece originated
        string targetAddress = (pecaSelecionada.filial_origem == "Matriz") ? address : sisteraddresses[sisternames.IndexOf(pecaSelecionada.filial_origem)];
        using UnityWebRequest req = UnityWebRequest.Put($"http://{targetAddress}/peca/" + pecaSelecionada.id, json);
        req.SetRequestHeader("Content-Type", "application/json");
        req.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeaders(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set($"Imprevisto ao atualizar peça de {pecaSelecionada.filial_origem}: {req.error}");
            msg.SetColor(true);
        } else {
            msg.Set("Peça atualizada com sucesso!");
            msg.SetColor(false);
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
        // Send update to the server where the PC originated
        string targetAddress = (PCSelecionado.filial_origem == "Matriz") ? address : sisteraddresses[sisternames.IndexOf(PCSelecionado.filial_origem)];
        using UnityWebRequest req = UnityWebRequest.Put($"http://{targetAddress}/computador/" + PCSelecionado.id, json);
        req.SetRequestHeader("Content-Type", "application/json");
        req.downloadHandler = new DownloadHandlerBuffer();
        AddAuthHeaders(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            msg.Set($"Imprevisto ao atualizar computador de {PCSelecionado.filial_origem}: {req.error}");
            msg.SetColor(true);
        } else {
            btnSalvar.interactable = false;
            SetInputFieldsInteractable(false);
            PopularDropdownTipos();
            AtualizarListaVisual();
        }
    }
}