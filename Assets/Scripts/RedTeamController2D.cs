using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RedTeamController2D : MonoBehaviour
{
    [Tooltip("Assign exactly 3 Red disks (order = cycle order)")]
    public List<PlayerDiskController2D> disks = new();

    [Tooltip("Cycle key for Red")]
    public Key cycleKey = Key.RightCtrl;

    private int activeIndex = 0;

    void OnEnable()
    {
        for (int i = 0; i < disks.Count; i++)
        {
            bool active = (i == activeIndex);
            if (!disks[i]) continue;
            disks[i].playerId = 2; // Red
            disks[i].SetMoveDir(Vector2.zero, active);
            if (!active) disks[i].Halt();
            disks[i].OnDestroyed += HandleDiskDestroyed;
        }
    }

    void OnDisable()
    {
        foreach (var d in disks)
            if (d) d.OnDestroyed -= HandleDiskDestroyed;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || disks.Count == 0) return;

        // Arrow-key input
        Vector2 dir = new Vector2(
            (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0),
            (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0)
        ).normalized;

        // Cycle active disk
        if (kb[cycleKey].wasPressedThisFrame)
        {
            int prev = activeIndex;
            activeIndex = (activeIndex + 1) % disks.Count;
            if (disks[prev]) disks[prev].Halt();
        }

        // Feed movement only to the active disk
        for (int i = 0; i < disks.Count; i++)
        {
            bool isActive = (i == activeIndex);
            if (!disks[i]) continue;
            disks[i].SetMoveDir(isActive ? dir : Vector2.zero, isActive);
            if (!isActive) disks[i].Halt();
        }
    }

    private void HandleDiskDestroyed(PlayerDiskController2D dead)
    {
        int idx = disks.IndexOf(dead);
        if (idx >= 0) disks.RemoveAt(idx);
        if (disks.Count == 0) return; // team wiped

        if (idx < activeIndex) activeIndex--;
        activeIndex = Mathf.Clamp(activeIndex, 0, disks.Count - 1);
        for (int i = 0; i < disks.Count; i++)
            disks[i].SetMoveDir(Vector2.zero, i == activeIndex);
    }
}
