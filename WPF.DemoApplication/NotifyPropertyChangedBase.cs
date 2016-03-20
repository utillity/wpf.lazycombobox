using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace uTILLIty.WPF.Demo
{
	/// <summary>
	///   A default implemenation of the <see cref="INotifyPropertyChanged" /> contract
	/// </summary>
	public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
	{
		private readonly Dictionary<string, object> _propValues = new Dictionary<string, object>();

		/// <summary>
		///   The event raised when a property of the inheriting instance has changed
		/// </summary>
		public virtual event PropertyChangedEventHandler PropertyChanged;

		public bool HasPropertyBeenSet(string propertyName)
		{
			// ReSharper disable once InconsistentlySynchronizedField
			return _propValues.ContainsKey(propertyName);
		}

		/// <summary>
		///   Raises the <see cref="PropertyChanged" /> event on the UI thread
		/// </summary>
		// ReSharper disable once MemberCanBeProtected.Global
		protected internal virtual void RaisePropertyChanged([CallerMemberName] string propName = null)
		{
			var ev = PropertyChanged;
			if (ev == null || string.IsNullOrEmpty(propName))
				return;

			var args = new PropertyChangedEventArgs(propName);
			ev(this, args);
		}

		/// <summary>
		///   Updates the <paramref name="value" /> of the specified <paramref name="propertyName">property</paramref>
		/// </summary>
		/// <returns>True, if the value has changed, else False</returns>
		protected virtual bool SetValue(object value, [CallerMemberName] string propertyName = null,
			bool unifyStringValue = true, bool raiseEvents = true, bool asyncEvents = false)
		{
			if (string.IsNullOrEmpty(propertyName))
				// ReSharper disable once LocalizableElement
				throw new ArgumentException("propertyName may not be null or empty", nameof(propertyName));

			lock (_propValues)
			{
				if (_propValues.ContainsKey(propertyName))
				{
					if (unifyStringValue)
					{
						var text = value as string;
						//empty string to null, remove surrounding whitespace
						if (text != null)
							value = string.Empty.Equals(text) ? null : text.Trim(' ', '\r', '\n', '\t');
					}
					var curValue = _propValues[propertyName];
					if (Equals(curValue, value))
						return false;
					if (raiseEvents)
					{
						if (asyncEvents)
							Task.Run(() => OnBeforePropertyChanging(propertyName, curValue, value));
						else
							OnBeforePropertyChanging(propertyName, curValue, value);
					}
					_propValues[propertyName] = value;
				}
				else
				{
					_propValues.Add(propertyName, value);
				}

				if (raiseEvents)
				{
					// ReSharper disable ExplicitCallerInfoArgument
					if (asyncEvents)
						Task.Run(() => RaisePropertyChanged(propertyName));
					else
						RaisePropertyChanged(propertyName);
					// ReSharper restore ExplicitCallerInfoArgument
				}
				return true;
			}
		}

		protected virtual void OnBeforePropertyChanging(string propertyName, object oldValue, object newValue)
		{
		}

		protected T GetValue<T>(T defaultValue = default(T), [CallerMemberName] string propertyName = null)
		{
			if (string.IsNullOrEmpty(propertyName))
				// ReSharper disable once LocalizableElement
				throw new ArgumentException("propertyName may not be null or empty", nameof(propertyName));

			lock (_propValues)
			{
				if (_propValues.ContainsKey(propertyName))
					return (T) _propValues[propertyName];
				return defaultValue;
			}
		}
	}
}