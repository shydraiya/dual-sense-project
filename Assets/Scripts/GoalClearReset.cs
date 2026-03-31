using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalClearReset : MonoBehaviour
{
    [Header("Goal")]
    public string playerTag = "Player";
    public bool useTrigger = true;

    [Header("Clear UI")]
    public string clearMessage = "GAME CLEAR!";
    public float resetDelay = 3f;
    public int fontSize = 40;
    public Color textColor = Color.white;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.7f);

    private bool isCleared;
    private float resetTime;
    private GUIStyle messageStyle;
    private GUIStyle shadowStyle;

    private void Update()
    {
        if (!isCleared)
        {
            return;
        }

        if (Time.unscaledTime >= resetTime)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger)
        {
            return;
        }

        TryClear(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTrigger)
        {
            return;
        }

        TryClear(collision.gameObject);
    }

    private void TryClear(GameObject otherObject)
    {
        if (isCleared || !otherObject.CompareTag(playerTag))
        {
            return;
        }

        isCleared = true;
        resetTime = Time.unscaledTime + resetDelay;
    }

    private void OnGUI()
    {
        if (!isCleared)
        {
            return;
        }

        EnsureStyles();

        Rect messageRect = new Rect(0f, 0f, Screen.width, Screen.height);
        Rect shadowRect = new Rect(3f, 3f, Screen.width, Screen.height);

        GUI.Label(shadowRect, clearMessage, shadowStyle);
        GUI.Label(messageRect, clearMessage, messageStyle);
    }

    private void EnsureStyles()
    {
        if (messageStyle == null)
        {
            messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.fontSize = fontSize;
            messageStyle.fontStyle = FontStyle.Bold;
            messageStyle.normal.textColor = textColor;
        }
        else
        {
            messageStyle.fontSize = fontSize;
            messageStyle.normal.textColor = textColor;
        }

        if (shadowStyle == null)
        {
            shadowStyle = new GUIStyle(messageStyle);
        }

        shadowStyle.alignment = TextAnchor.MiddleCenter;
        shadowStyle.fontSize = fontSize;
        shadowStyle.fontStyle = FontStyle.Bold;
        shadowStyle.normal.textColor = shadowColor;
    }
}
