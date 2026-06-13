using UnityEngine;

// Cong cu test tu dong: cho player tu chay sang phai va canh thoi diem nhay
// de dap dau enemy. Chi dung de kiem thu, tat bang active=false.
public class AutoPilot : MonoBehaviour
{
    public bool active = true;
    public float jumpMinDx = 5.5f;
    public float jumpMaxDx = 6.6f;

    float jumpCooldown;
    float lastX;
    float stuckTime;

    void Update()
    {
        if (!active) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        if (gm.player.IsDead)
        {
            gm.player.simMove = 0f;
            return;
        }

        gm.player.simMove = 1f;

        jumpCooldown -= Time.deltaTime;

        // Bi ket tuong (khong tien len duoc) thi nhay qua
        float x = gm.player.transform.position.x;
        if (x - lastX < 0.01f) stuckTime += Time.deltaTime;
        else stuckTime = 0f;
        lastX = x;
        if (stuckTime > 0.3f && jumpCooldown <= 0f)
        {
            gm.player.simJump = true;
            jumpCooldown = 0.8f;
            stuckTime = 0f;
            return;
        }
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            float dx = e.transform.position.x - gm.player.transform.position.x;
            if (dx > jumpMinDx && dx < jumpMaxDx && jumpCooldown <= 0f)
            {
                gm.player.simJump = true;
                jumpCooldown = 1.5f;
                break;
            }
        }
    }
}
