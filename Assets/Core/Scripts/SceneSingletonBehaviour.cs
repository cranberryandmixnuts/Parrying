using UnityEngine;

public class SceneSingletonBehaviour<TSelf> : MonoBehaviour where TSelf : SceneSingletonBehaviour<TSelf> {

	//======================================================================| Fields

	private static TSelf _instance;

	//======================================================================| Properties

	public static TSelf Instance {

		get {

			if (_instance == null) {
				_instance = FindFirstObjectByType<TSelf>();
			}

			if (!Application.isPlaying && _instance == null) {
				Debug.LogWarning($"[SceneSingleton] {typeof(TSelf).Name} not found in scene.");
			}

			return _instance;

		}

	}

	//======================================================================| Unity Behaviours

	protected virtual void Awake() {

		if (_instance == null) {
			_instance = this as TSelf;
		}
		else if (_instance != this) {
			Destroy(gameObject);
		}

	}

	protected virtual void OnDestroy() {
		if (_instance == this) {
			_instance = null;
		}
	}

}