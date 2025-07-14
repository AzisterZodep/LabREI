using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Reflection;

public class ServidorAPI : MonoBehaviour
{
    [SerializeField] private string servidorURL = "http://192.168.1.210:5000"; // IP do servidor
    [SerializeField] private List<string> cache;
    public Button btn_conclude;

    private void Awake()
    {
        if (cache == null)
        {
            cache = new List<string>();
        }
    }
    public void CacheDelete()
    {
        cache.Clear();
        Debug.Log("Cache has been cleared.");
    }
    public void AddToCache(string item)
    {
        if (!cache.Contains(item))
        {
            cache.Add(item);
            Debug.Log($"'{item}' added to cache.");
        }
        else
        {
            Debug.Log($"'{item}' is already in the cache.");
        }
    }
    public void RemoveFromCache(string item)
    {
        if (cache.Contains(item))
        {
            cache.Remove(item);
            Debug.Log($"'{item}' removed from cache.");
        }
        else
        {
            Debug.Log($"'{item}' not found in cache.");
        }
    }

    public void ButtonSetFuction(string text)
    {
        btn_conclude.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(text)){
            MethodInfo method = GetType().GetMethod(text, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null){
                btn_conclude.onClick.AddListener(() => method.Invoke(this, null));
            }else{
                Debug.LogWarning($"Função '{text}' não encontrada em '{btn_conclude.name}'. Verifique o nome e a visibilidade (public/private).");
            }
        }
    }

    public void ButtonResetFuction(){
        btn_conclude.onClick.RemoveAllListeners();
        btn_conclude.onClick.AddListener(() => btn_conclude.gameObject.SetActive(false));
    }
    
    // CADASTRAR COMPUTADOR
    public void CadastrarComputador()
    {
        StartCoroutine(EnviarComputador(cache[0], cache[1]));
    }

    private IEnumerator EnviarComputador(string nome, string origem)
    {
        string json = "{\"nome\":\"" + nome + "\",\"origem\":\"" + origem + "\"}";
        byte[] corpo = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(servidorURL + "/computador", "POST");
        request.uploadHandler = new UploadHandlerRaw(corpo);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Computador cadastrado: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erro ao cadastrar computador: " + request.error);
        }
    }

    // CADASTRAR PEÇA
    public void CadastrarPeca(string tipo, string modelo, string[] compatibilidades)
    {
        StartCoroutine(EnviarPeca(tipo, modelo, compatibilidades));
    }

    private IEnumerator EnviarPeca(string tipo, string modelo, string[] compat)
    {
        string compatStr = string.Join("\",\"", compat);
        string json = $"{{\"tipo\":\"{tipo}\",\"modelo\":\"{modelo}\",\"compatibilidade\":[\"{compatStr}\"]}}";
        byte[] corpo = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(servidorURL + "/peca", "POST");
        request.uploadHandler = new UploadHandlerRaw(corpo);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Peça cadastrada: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erro ao cadastrar peça: " + request.error);
        }
    }

    // DESMONTAR COMPUTADOR
    public void DesmontarComputador(int id)
    {
        StartCoroutine(Desmontar(id));
    }

    private IEnumerator Desmontar(int id)
    {
        UnityWebRequest request = UnityWebRequest.PostWwwForm(servidorURL + "/desmontar/" + id, "");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Computador desmontado: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erro ao desmontar: " + request.error);
        }
    }

    // MONTAR COMPUTADOR AUTOMATICAMENTE
    public void MontarComputador()
    {
        StartCoroutine(Montar());
    }

    private IEnumerator Montar()
    {
        UnityWebRequest request = UnityWebRequest.Get(servidorURL + "/montar");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Montagem automática: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erro na montagem: " + request.error);
        }
    }

    // VER PEÇAS FALTANTES
    public void VerFaltantes()
    {
        StartCoroutine(Faltantes());
    }

    private IEnumerator Faltantes()
    {
        UnityWebRequest request = UnityWebRequest.Get(servidorURL + "/faltantes");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Peças faltando: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Erro ao buscar faltantes: " + request.error);
        }
    }
}
