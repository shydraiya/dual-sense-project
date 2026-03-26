using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHapticByDirection : MonoBehaviour
{
    public Transform player;
    public Transform enemy;

    public float maxDistance = 10f;   // 이 거리보다 멀면 진동 0
    public float maxMotorPower = 1f;  // 최대 진동 세기

    void Update()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null || player == null || enemy == null)
            return;

        Vector3 toEnemy = enemy.position - player.position;
        float distance = toEnemy.magnitude;

        if (distance > maxDistance)
        {
            pad.SetMotorSpeeds(0f, 0f);
            return;
        }

        // 거리 기반 세기: 가까울수록 1에 가까움
        float strength = 1f - Mathf.Clamp01(distance / maxDistance);
        strength *= strength;
        strength *= maxMotorPower;

        // 플레이어 기준 오른쪽 방향과 비교
        float side = Vector3.Dot(player.right, toEnemy.normalized);

        float leftMotor = 0f;
        float rightMotor = 0f;

        if (side < 0f)
        {
            // 적이 왼쪽
            leftMotor = strength;
        }
        else
        {
            // 적이 오른쪽
            rightMotor = strength;
        }

        pad.SetMotorSpeeds(leftMotor, rightMotor);
    }

    private void OnDisable()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}