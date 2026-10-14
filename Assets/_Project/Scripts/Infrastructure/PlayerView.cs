using AssistedPass.Domain;
using UnityEngine;

namespace AssistedPass.Infrastructure
{
    /// <summary>
    /// The capsule in the scene. Creates its domain Player and copies its position every frame,
    /// so you can move players in the Scene view during Play and the system reacts.
    /// </summary>
    [DefaultExecutionOrder(-100)] // before everyone else, so the domain has up-to-date positions
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] Team team = Team.Home;
        [Tooltip("Unique id of the player within the scene.")]
        [SerializeField] int number;

        public Player Model { get; private set; }

        void Awake() => Model = new Player(number, team, PitchConversion.ToPitch(transform.position));

        void Update() => Model.Position = PitchConversion.ToPitch(transform.position);

        public void LookTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
