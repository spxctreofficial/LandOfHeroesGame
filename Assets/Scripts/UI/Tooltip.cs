using System.Collections;
using System.Collections.Generic;
using CodeMonkey;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode()]
public class Tooltip : MonoBehaviour
{
    public TMP_Text header;
    public TMP_Text body;
    public LayoutElement layoutElement;
    public int characterWrapLimit;

    public RectTransform rectTransform;

    public void SetText(string body = "", string header = "")
    {
        if (string.IsNullOrEmpty(body) && string.IsNullOrEmpty(header))
        {
            Debug.Log("Dont call me baby");
            return;
        }
        switch (string.IsNullOrEmpty(header))
        {
            case true:
                this.header.gameObject.SetActive(false);
                break;
            case false:
                this.header.gameObject.SetActive(true);
                this.header.text = header;
                break;
        }
        switch (string.IsNullOrEmpty(body))
        {
            case true:
                this.body.gameObject.SetActive(false);
                break;
            case false:
                this.body.gameObject.SetActive(true);
                this.body.text = body;
                break;
        }
        
        int headerLength = this.header.text.Length;
        int contentLength = this.body.text.Length;

        layoutElement.enabled = headerLength > characterWrapLimit || contentLength > characterWrapLimit ? true : false;
    }
    public void UpdatePosition()
    {
        var mousePos = Input.mousePosition;
        mousePos.z = transform.parent.GetComponent<Canvas>().planeDistance;
        transform.position = FindObjectOfType<Camera>().ScreenToWorldPoint(mousePos);

        
        var pivot = new Vector2(mousePos.x / Screen.width, mousePos.y > Screen.height / 2f ? 1.5f : 0f);
        pivot.y = GameController.instance.gamePhase == GamePhase.GameStart ? 0f : pivot.y;
        rectTransform.pivot = pivot;
    }

    private void Awake()
    {
        if (gameObject.GetComponent<CanvasGroup>() == null) gameObject.AddComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }
    private void Update()
    {
        if (Application.isEditor)
        {
            int headerLength = header.text.Length;
            int contentLength = body.text.Length;

            layoutElement.enabled = headerLength > characterWrapLimit || contentLength > characterWrapLimit ? true : false;
        }

        if (!Application.isPlaying) return;
        
        UpdatePosition();
    }
}
