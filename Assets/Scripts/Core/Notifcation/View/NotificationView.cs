using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class NotificationView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text messageText;
    [Header("Colors")]
    [SerializeField] private Color errorColor=new Color(1,.2f,.16f);
    [SerializeField] private Color successColor=new Color(.25f,1,.16f);
    [SerializeField] private Color infoColor=new Color(1,.86f,.12f);
    [Header("Layout")]
    [SerializeField] private float maxWidth=900;
    [SerializeField] private float horizontalPadding=24,verticalPadding=12;
    [Header("Animation")]
    [SerializeField] private bool useAnimation=true;
    [SerializeField] private float showDuration=.2f,hideDuration=.18f,lifetime=3,startScale=.94f;
    [SerializeField] private Ease showEase=Ease.OutBack,hideEase=Ease.InQuad;
    RectTransform rect;
    CanvasGroup canvasGroup;
    Sequence showSequence,hideSequence;
    float hideAt=-1;
    public NotificationManager Owner {get;set;}
    public string Message=>messageText!=null?messageText.text:"";
    public Color TextColor=>messageText!=null?messageText.color:Color.white;
    void Awake()
    {
        rect=GetComponent<RectTransform>();canvasGroup=GetComponent<CanvasGroup>();
        if(canvasGroup==null)canvasGroup=gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts=false;canvasGroup.interactable=false;
    }
    void OnEnable(){showSequence?.Kill();hideSequence?.Kill();hideAt=-1;}
    public void Setup(Sprite icon,string text,NotificationType type)
    {
        if(messageText!=null){messageText.text=text;messageText.color=type==NotificationType.Error?errorColor:type==NotificationType.Success?successColor:infoColor;}
        bool hasIcon=iconImage!=null&&icon!=null;
        if(iconImage!=null){iconImage.gameObject.SetActive(hasIcon);iconImage.sprite=icon;}
        if(messageText!=null)
        {
            float iconSpace=hasIcon?56:0;
            var preferred=messageText.GetPreferredValues(text,maxWidth-horizontalPadding*2-iconSpace,Mathf.Infinity);
            float width=Mathf.Clamp(preferred.x+horizontalPadding*2+iconSpace,220,maxWidth);
            float height=Mathf.Max(60,preferred.y+verticalPadding*2);
            var layout=GetComponent<LayoutElement>();
            if(layout!=null){layout.preferredWidth=width;layout.preferredHeight=height;}
            rect.sizeDelta=new Vector2(width,height);
            var labelRect=messageText.rectTransform;labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;
            labelRect.offsetMin=new Vector2(horizontalPadding+iconSpace,verticalPadding);labelRect.offsetMax=new Vector2(-horizontalPadding,-verticalPadding);
        }
        rect.localScale=Vector3.one;canvasGroup.alpha=1;
        if(useAnimation)
        {
            rect.localScale=Vector3.one*startScale;canvasGroup.alpha=0;
            showSequence=DOTween.Sequence().Join(rect.DOScale(1,showDuration).SetEase(showEase)).Join(canvasGroup.DOFade(1,showDuration)).SetUpdate(true);
        }
        hideAt=lifetime>0?Time.unscaledTime+lifetime:-1;
    }
    void Update(){if(hideAt>=0&&Time.unscaledTime>=hideAt)StartHide();}
    public void StartHide()
    {
        if(!gameObject.activeInHierarchy)return;
        hideAt=-1;showSequence?.Kill();hideSequence?.Kill();
        if(!useAnimation){HideImmediately();return;}
        hideSequence=DOTween.Sequence().Join(canvasGroup.DOFade(0,hideDuration).SetEase(hideEase)).Join(rect.DOScale(.96f,hideDuration)).OnComplete(HideImmediately).SetUpdate(true);
    }
    public void HideImmediately(){gameObject.SetActive(false);}
    void OnDisable(){showSequence?.Kill();hideSequence?.Kill();hideAt=-1;Owner?.RegisterHidden(this);}
    void OnDestroy(){showSequence?.Kill();hideSequence?.Kill();}
}
