using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuzerainUnbound;

public class ReadAllController : MonoBehaviour
{
    public ReadAllController(IntPtr ptr) : base(ptr) { }

    private string _notification = "";
    private float _notificationTimer = 0f;
    private GUIStyle _notificationStyle;

    private void ShowNotification(string msg)
    {
        _notification = msg;
        _notificationTimer = 3f;
    }

    void OnGUI()
    {
        if (_notificationTimer <= 0) { return; }
        if (_notificationStyle == null)
        {
            _notificationStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleLeft };
            _notificationStyle.normal.textColor = Color.white;
        }
        GUI.Box(new Rect(Screen.width - 400, 20, 380, 32), _notification, _notificationStyle);
    }

    void Update()
    {
        if (_notificationTimer > 0) { _notificationTimer -= Time.deltaTime; }
        if (Keyboard.current == null) { return; }

        if (Keyboard.current[InputHelper.ToInputKey(Plugin.ReadAllMapReportsKey.Value)].wasPressedThisFrame)
        {
            var gfm = FindObjectOfType<GameFlowManager>();
            var journalPanel = FindObjectOfType<JournalPanel>();
            int count = 0;
            if (gfm != null)
            {
                unsafe
                {
                    nint listPtr = *(nint*)(gfm.Pointer + 0x78);
                    if (listPtr != 0)
                    {
                        var activeReports = new Il2CppSystem.Collections.Generic.List<ReportData>(listPtr);
                        var snapshot = new System.Collections.Generic.List<ReportData>();
                        for (int i = 0; i < activeReports.Count; i++)
                        {
                            snapshot.Add(activeReports[i]);
                        }
                        foreach (var report in snapshot)
                        {
                            report.IsDone = true;
                            activeReports.Remove(report);
                            journalPanel?.AddReportToJournalReportPage(report);
                            count++;
                        }
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                StrangeRanks.IncrementStrangeStat(Plugin.ReportsReadCount, "Report Reader");
            }
            ShowNotification($"Read All Reports: Marked {count} map report(s) as done.");
            Plugin.Log.LogInfo($"[ReadAllReports] Marked {count} map report(s) as done.");
            FindObjectOfType<TokenIndicatorPanel>()?.Setup();
        }

        if (Keyboard.current[InputHelper.ToInputKey(Plugin.ReadAllArticlesKey.Value)].wasPressedThisFrame)
        {
            var news = EntityDataManager.NewsData;
            if (news != null)
            {
                var gfm = FindObjectOfType<GameFlowManager>();
                int currentTurn = gfm?.CurrentTurnNo ?? -1;
                int count = 0;
                foreach (var article in news)
                {
                    if (!article.IsRead && article.IsEnabled)
                    {
                        bool isCurrentTurn = false;
                        unsafe
                        {
                            nint subObjPtr = *(nint*)(article.Pointer + 0x38);
                            if (subObjPtr != 0)
                            {
                                isCurrentTurn = *(int*)(subObjPtr + 0x10) == currentTurn;
                            }
                        }
                        if (isCurrentTurn)
                        {
                            article.IsRead = true;
                            count++;
                        }
                    }
                }
                for (int i = 0; i < count; i++)
                {
                    StrangeRanks.IncrementStrangeStat(Plugin.ReportsReadCount, "Report Reader");
                }
                ShowNotification($"Read All Reports: Marked {count} news article(s) as read.");
                Plugin.Log.LogInfo($"[ReadAllReports] Marked {count} news article(s) as read.");
                FindObjectOfType<NewsPanel>()?.Setup();
            }
        }
    }
}
