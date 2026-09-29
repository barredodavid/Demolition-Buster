using System;
using UnityEngine.UIElements;

namespace DemolitionBuster.UI
{
    /// <summary>Base class for a screen bound to a UXML subtree. Screens are plain C# presenters, not MonoBehaviours.</summary>
    public abstract class UIScreen
    {
        private const string HiddenClass = "hidden";

        protected VisualElement Root { get; }

        public bool IsVisible => !Root.ClassListContains(HiddenClass);

        protected UIScreen(VisualElement root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public void SetVisible(bool visible) => Root.EnableInClassList(HiddenClass, !visible);

        /// <summary>Queries a required element and fails loudly if the UXML and the code drift apart.</summary>
        protected T Require<T>(string name) where T : VisualElement
        {
            return Root.Q<T>(name) ?? throw new InvalidOperationException($"{GetType().Name}: element '{name}' of type {typeof(T).Name} not found in UXML.");
        }
    }
}
