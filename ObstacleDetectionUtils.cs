using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObstacleDetectionUtils
{
    public static readonly Vector2[] eightDirections = new Vector2[]
    {
        new Vector2(1, 0),  // Direita
        new Vector2(1, 1),  // Cima-direita
        new Vector2(0, 1),  // Cima
        new Vector2(-1, 1), // Cima-esquerda
        new Vector2(-1, 0), // Esquerda
        new Vector2(-1, -1),// Baixo-esquerda
        new Vector2(0, -1), // Baixo
        new Vector2(1, -1)  // Baixo-direita
    };

    public static Vector2 GetAvoidanceDirection(Transform self, Vector2 desiredDirection, float obstacleRadius, float avoidanceRadius, LayerMask obstacleMask)
    {
        int rayCount = eightDirections.Length;
        float[] interest = new float[rayCount];
        float[] danger = new float[rayCount];
        float[] result = new float[rayCount];

        for (int i = 0; i < rayCount; i++)
            interest[i] = Mathf.Max(0, Vector2.Dot(desiredDirection.normalized, eightDirections[i]));

        Collider2D[] obstacles = Physics2D.OverlapCircleAll(self.position, obstacleRadius, obstacleMask);
        foreach (var obs in obstacles)
        {
            Vector2 dirToObs = (obs.ClosestPoint(self.position) - (Vector2)self.position);
            float dist = dirToObs.magnitude;
            float custo = dist <= avoidanceRadius ? 1 : (obstacleRadius - dist) / obstacleRadius;
            Vector2 normDir = dirToObs.normalized;

            for (int j = 0; j < rayCount; j++)
            {
                float dot = Vector2.Dot(normDir, eightDirections[j]);
                danger[j] = Mathf.Max(danger[j], dot * custo);
            }
        }

        Vector2 finalDirection = Vector2.zero;
        for (int i = 0; i < rayCount; i++)
        {
            result[i] = Mathf.Clamp01(interest[i] - danger[i]);
            finalDirection += eightDirections[i] * result[i];
        }

        return finalDirection.magnitude > 0.1f ? finalDirection.normalized : desiredDirection.normalized;
    }
}