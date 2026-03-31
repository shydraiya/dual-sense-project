using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHapticByDirection : MonoBehaviour
{
    public Transform player;
    public List<Transform> enemies = new List<Transform>();
    public bool autoFindEnemiesByTag;
    public string enemyTag = "Enemy";

    public float maxDistance = 10f;
    public float maxMotorPower = 1f;

    private void Update()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null || player == null)
        {
            return;
        }

        float leftMotor = 0f;
        float rightMotor = 0f;

        IEnumerable<Transform> enemyTargets = GetEnemyTargets();
        foreach (Transform enemy in enemyTargets)
        {
            if (enemy == null)
            {
                continue;
            }

            Vector3 toEnemy = enemy.position - player.position;
            float distance = toEnemy.magnitude;
            if (distance > maxDistance || distance <= 0.001f)
            {
                continue;
            }

            float strength = 1f - Mathf.Clamp01(distance / maxDistance);
            strength *= strength;
            strength *= maxMotorPower;

            float side = Vector3.Dot(player.right, toEnemy.normalized);
            if (side < 0f)
            {
                leftMotor += strength;
            }
            else
            {
                rightMotor += strength;
            }
        }

        pad.SetMotorSpeeds(
            Mathf.Clamp01(leftMotor),
            Mathf.Clamp01(rightMotor));
    }

    private IEnumerable<Transform> GetEnemyTargets()
    {
        if (autoFindEnemiesByTag)
        {
            GameObject[] taggedEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
            foreach (GameObject enemyObject in taggedEnemies)
            {
                yield return enemyObject.transform;
            }

            yield break;
        }

        foreach (Transform enemy in enemies)
        {
            yield return enemy;
        }
    }

    private void OnDisable()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}
