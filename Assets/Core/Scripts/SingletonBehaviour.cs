using UnityEngine;

public class SingletonBehaviour<TSelf> : MonoBehaviour where TSelf : SingletonBehaviour<TSelf> {

	//======================================================================| Fields

	private static TSelf _instance;
	private static bool _isApplicationQuitting = false;

	protected static bool _disableInstantiateLog = false;
	protected static string _customInstanceName = null;

	//======================================================================| Properties

	public static TSelf Instance {

		get {
			
			if (_isApplicationQuitting) {
				Debug.LogWarning(
					$"[Singleton] " +
					$"Instance of {typeof(TSelf)} already destroyed on application quit."
				);
				return null;
			}

			if (_instance == null) {
				_instance = FindFirstObjectByType<TSelf>();
			}

			if (_instance == null) {

				if (!_disableInstantiateLog) {
					Debug.LogWarning(
						$"[Singleton] " +
						$"No instance of {typeof(TSelf).Name} found in scene. " +
						$"Creating new game object instance"
					);
				}

				if (Application.isPlaying) {
					string name = _customInstanceName ??= typeof(TSelf).Name;
					_instance = new GameObject(name).AddComponent<TSelf>();
				}

			}			

			return _instance;

		}

	}

	//======================================================================| Unity Behaviours

	protected virtual void Awake() {

		if (_instance == null) {
			_instance = this as TSelf;
			DontDestroyOnLoad(gameObject);
		}

		else if (_instance != this) {
			Destroy(gameObject);
		}

	}

	protected virtual void OnApplicationQuit() {
		_isApplicationQuitting = true;	
	}

}