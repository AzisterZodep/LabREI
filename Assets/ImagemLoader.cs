using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

public class ImagemLoader : MonoBehaviour
{
    public Image imagemUI; // Arraste um Image da UI aqui no inspetor
    private string servidorURL = "http://192.168.1.210:5000"; // IP do seu servidor

    public void CarregarImagemDaPeca(int id)
    {
        StartCoroutine(BaixarImagem(id));
    }

    IEnumerator BaixarImagem(int id)
    {
        string url = servidorURL + "/imagem/peca/" + id;
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D textura = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(
                textura,
                new Rect(0, 0, textura.width, textura.height),
                new Vector2(0.5f, 0.5f)
            );
            imagemUI.sprite = sprite;
        }
        else
        {
            Debug.LogError("Erro ao carregar imagem: " + request.error);
        }
    }
}
