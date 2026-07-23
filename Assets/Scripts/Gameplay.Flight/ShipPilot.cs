using UnityEngine;

namespace Game.Gameplay.Flight
{
    // Общая "пилотная" математика для кораблей под управлением AI (гонщики,
    // боевые). Чистые функции без состояния — значения приходят параметрами
    // из конкретного AI, поэтому сериализованные настройки остаются на своих
    // местах (на префабе гонщика или в EnemyCombatConfig).
    public static class ShipPilot
    {
        // PD-регулятор поворота: доворот на цель минус демпфирование текущего
        // вращения. signedAngle — угол от носа (transform.right) до цели.
        public static float Steer(Rigidbody2D body, float signedAngle, float responsiveness, float damping)
        {
            var correction = signedAngle * responsiveness - body.angularVelocity * damping;
            return Mathf.Clamp(correction / 90f, -1f, 1f);
        }

        // Скорость сближения с целью: >0 — приближаемся, <0 — отдаляемся.
        public static float ClosingSpeed(Rigidbody2D body, Vector2 toTarget, float distance)
        {
            return distance > 0.01f
                ? Vector2.Dot(body.linearVelocity, toTarget / distance)
                : 0f;
        }

        // Обход препятствий тремя лучами (прямо и ±angle). Препятствие справа —
        // доворот влево (+), слева — вправо (-).
        public static float ObstacleAvoidance(Transform self, LayerMask mask, float sensorLength, float sensorAngle)
        {
            var avoidance = 0f;

            if (SensorHit(self, mask, sensorLength, -sensorAngle))
            {
                avoidance += 1f;
            }

            if (SensorHit(self, mask, sensorLength, sensorAngle))
            {
                avoidance -= 1f;
            }

            if (SensorHit(self, mask, sensorLength, 0f))
            {
                avoidance += avoidance >= 0f ? 1f : -1f;
            }

            return avoidance;
        }

        private static bool SensorHit(Transform self, LayerMask mask, float length, float angle)
        {
            var direction = Quaternion.Euler(0f, 0f, angle) * self.right;
            var hit = Physics2D.Raycast(self.position, direction, length, mask);
            return hit.collider != null && !hit.collider.transform.IsChildOf(self);
        }
    }
}
