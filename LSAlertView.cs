using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;

namespace Depot.iOS {
	/// <summary>
	/// LS alert view. Based on https://github.com/wimagguc/ios-custom-alertview
	/// </summary>
	public class LSAlertView : UIView {
		public event EventHandler<ButtonTouchedEventArgs> ButtonTouched;

		private const float _defaultButtonHeight = 44f;
		private const float _defaultButtonSpacerHeight = 1f;
		private const float _cornerRadius = 7f;
		private const float _motionEffectExtent = 10f;
		private float _buttonHeight = 0f;
		private float _buttonSpacerHeight = 0f;
		private UIView _parentView;
		private UIView _dialogView;
		private UIView _containerView;
		private string[] _buttonTitles;
		private bool _motionEffectsEnabled;
		private NSObject _orientationDidChangeObserver;
		private NSObject _keyboardWillShowObserver;
		private NSObject _keyboardWillHideObserver;

		public UIView ContainerView {
			get { return _containerView; }
			set { _containerView = value; }
		}

		public bool MotionEffectsEnabled {
			get { return _motionEffectsEnabled; }
			set { _motionEffectsEnabled = value; }
		}

		public string[] ButtonTitles {
			get { return _buttonTitles; }
			set { _buttonTitles = value; }
		}

		public LSAlertView () : base (RectangleF.Empty) {
			var width = UIScreen.MainScreen.Bounds.Width;
			var height = UIScreen.MainScreen.Bounds.Height;
			Frame = new RectangleF (0f, 0f, width, height);

			_motionEffectsEnabled = true;
			_buttonTitles = new string[] { "Close" };

			_orientationDidChangeObserver = 
				UIDevice.Notifications.ObserveOrientationDidChange (OrientationDidChangeHandler);
			_keyboardWillShowObserver =
				UIKeyboard.Notifications.ObserveWillShow (KeyboardWillShowHandler);
			_keyboardWillHideObserver = 
				UIKeyboard.Notifications.ObserveWillHide (KeyboardWillHideHandler);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);

			_orientationDidChangeObserver.Dispose ();
			_keyboardWillShowObserver.Dispose ();
			_keyboardWillHideObserver.Dispose ();
		}
		//
		// TODO: would prefer using a async method that returns status when finished (Hide() is called)
		public void Show () {
			_dialogView = CreateContainerView ();

			_dialogView.Layer.ShouldRasterize = true;
			_dialogView.Layer.RasterizationScale = UIScreen.MainScreen.Scale;

			Layer.ShouldRasterize = true;
			Layer.RasterizationScale = UIScreen.MainScreen.Scale;

			if (_motionEffectsEnabled) {
				ApplyMotionEffects ();
			}

			_dialogView.Layer.Opacity = .5f;
			_dialogView.Layer.Transform = CATransform3D.MakeScale (1.3f, 1.3f, 1f);

			BackgroundColor = UIColor.FromWhiteAlpha (0f, 0f);

			AddSubview (_dialogView);

			// can be attached to a view or to the top most window
			// attached to a view:
			if (_parentView != null) {
				_parentView.AddSubview (this);

				// attached to the top most window (make sure we are using the right orientation):
			} else {
				var interfaceOrientation = UIApplication.SharedApplication.StatusBarOrientation;
				switch (interfaceOrientation) {
				case UIInterfaceOrientation.LandscapeLeft:
					Transform = CGAffineTransform.MakeRotation ((float)Math.PI * 270f / 180f);
					break;
				case UIInterfaceOrientation.LandscapeRight:
					Transform = CGAffineTransform.MakeRotation ((float)Math.PI * 90f / 180f);
					break;
				case UIInterfaceOrientation.PortraitUpsideDown:
					Transform = CGAffineTransform.MakeRotation ((float)Math.PI * 180f / 180f);
					break;
				default:
					break;
				}

				Frame = new RectangleF (0f, 0f, Frame.Width, Frame.Height);
				UIApplication.SharedApplication.Windows [0].AddSubview (this);
			}

			UIView.Animate (.2f, 0f, UIViewAnimationOptions.CurveEaseInOut, () => {
				BackgroundColor = UIColor.FromWhiteAlpha (0f, .4f);
				_dialogView.Layer.Opacity = 1.0f;
				_dialogView.Layer.Transform = CATransform3D.MakeScale (1, 1, 1);
			}, null);
		}

		public void Close () {
			var currentTransform = _dialogView.Layer.Transform;

			var startRotation = _dialogView.ValueForKeyPath (new NSString ("layer.transform.rotation.z")) as NSNumber;
			var rotation = CATransform3D.MakeRotation (-startRotation.FloatValue + (float)Math.PI * 270f / 180f, 0f, 0f, 0f);

			_dialogView.Layer.Transform = rotation.Concat (CATransform3D.MakeScale (1f, 1f, 1f));
			_dialogView.Layer.Opacity = 1f;

			UIView.Animate (.2f, 0f, UIViewAnimationOptions.TransitionNone, () => {
				BackgroundColor = UIColor.FromWhiteAlpha (0f, 0f);
				_dialogView.Layer.Transform = currentTransform.Concat (CATransform3D.MakeScale (.6f, .6f, 1f));
				_dialogView.Layer.Opacity = 0f;
			}, () => {
				foreach (var subview in Subviews) {
					subview.RemoveFromSuperview ();
				}

				RemoveFromSuperview ();
			});
		}

		#region Private methods methods

		void SetSubview (UIView subview) {
			_containerView = subview;
		}

		UIView CreateContainerView () {
			if (_containerView == null) {
				_containerView = new UIView (new RectangleF (0f, 0f, 300f, 150f));
			}

			var screenSize = CalculateScreenSize ();
			var dialogSize = CalculateDialogSize ();

			// for the black background
			Frame = new RectangleF (0f, 0f, screenSize.Width, screenSize.Height);

			// this is the dialog's container; we attach the custom content and the buttons to this one
			var x = (screenSize.Width - dialogSize.Width) / 2;
			var y = (screenSize.Height - dialogSize.Height) / 2;
			var dialogContainer = new UIView (new RectangleF (x, y, dialogSize.Width, dialogSize.Height));
			dialogContainer.ClipsToBounds = true;

			// first, we style the dialog to match the iOS7 UIAlertView >>>
			var gradient = new CAGradientLayer ();
			gradient.Frame = dialogContainer.Bounds;
			gradient.Colors = new CGColor[] {
				UIColor.FromWhiteAlpha (218f / 255f, 1f).CGColor, 
				UIColor.FromWhiteAlpha (233f / 255f, 1f).CGColor,
				UIColor.FromWhiteAlpha (218f / 255f, 1f).CGColor
			};

			var cornerRadius = _cornerRadius;
			gradient.CornerRadius = cornerRadius;
			dialogContainer.Layer.InsertSublayer (gradient, 0);

			dialogContainer.Layer.CornerRadius = cornerRadius;
			dialogContainer.Layer.BorderColor = UIColor.FromWhiteAlpha (198f / 255f, 1f).CGColor;
			dialogContainer.Layer.BorderWidth = 1;
			dialogContainer.Layer.ShadowRadius = cornerRadius + 5;
			dialogContainer.Layer.ShadowOpacity = 0.1f;
			dialogContainer.Layer.ShadowOffset = new SizeF (0 - (cornerRadius + 5) / 2, 0 - (cornerRadius + 5) / 2);
			dialogContainer.Layer.ShadowColor = UIColor.Black.CGColor;
			dialogContainer.Layer.ShadowPath = UIBezierPath.FromRoundedRect (dialogContainer.Bounds, dialogContainer.Layer.CornerRadius).CGPath;

			// There is a line above the button
			x = 0f;
			y = dialogContainer.Bounds.Height - _buttonHeight - _buttonSpacerHeight;
			var lineView = new UIView (new RectangleF (x, y, dialogContainer.Bounds.Width, _buttonSpacerHeight));
			lineView.BackgroundColor = UIColor.FromWhiteAlpha (198f / 255f, 1f);
			dialogContainer.AddSubview (lineView);
			// ^^^

			// Add the custom container if there is any
			dialogContainer.AddSubview (_containerView);

			// Add the buttons too
			AddButtons (dialogContainer);

			// Add vertical lines (TODO: should create a cleaner solution for adding lines)
			if (ButtonTitles.Length > 1) {
				var buttonWidth = dialogContainer.Bounds.Width / _buttonTitles.Length;
				y = dialogContainer.Bounds.Height - _buttonHeight;

				for (int i = 0; i < (ButtonTitles.Length - 1); i++) {
					x = i * buttonWidth + buttonWidth;

					var frame = new RectangleF (x, y, _buttonSpacerHeight, _buttonHeight);
					lineView = new UIView (frame);
					lineView.BackgroundColor = UIColor.FromWhiteAlpha (198f / 255f, 1f);
					dialogContainer.AddSubview (lineView);
				}
			}

			return dialogContainer;
		}

		void AddButtons (UIView container) {
			if (_buttonTitles == null /* || _buttonTitles.Length == 0 */) {
				return;
			}

			var buttonWidth = container.Bounds.Width / _buttonTitles.Length;

			for (var i = 0; i < _buttonTitles.Length; i++) {
				var closeButton = new LSAlertViewButton (UIButtonType.Custom);

				var x = i * buttonWidth;
				var y = container.Bounds.Height - _buttonHeight;
				closeButton.Frame = new RectangleF (x, y, buttonWidth, _buttonHeight);
				closeButton.TouchUpInside += CloseButtonTouchUpInside;
				closeButton.Tag = i;
				closeButton.SetTitle (_buttonTitles [i], UIControlState.Normal);
				closeButton.Layer.CornerRadius = _cornerRadius;
				closeButton.TitleLabel.Font = (i == (ButtonTitles.Length - 1)) 
					? UIFont.BoldSystemFontOfSize (16f) 
					: UIFont.SystemFontOfSize (16f);

				container.AddSubview (closeButton);
			}
		}

		void ApplyMotionEffects () {
			if (UIDevice.CurrentDevice.CheckSystemVersion (6, 1)) {
				return;
			}

			var horizontalEffect = new UIInterpolatingMotionEffect ("center.x",  
				                       UIInterpolatingMotionEffectType.TiltAlongHorizontalAxis);
			horizontalEffect.MinimumRelativeValue = new NSNumber (-_motionEffectExtent);
			horizontalEffect.MaximumRelativeValue = new NSNumber (_motionEffectExtent);

			var verticalEffect = new UIInterpolatingMotionEffect ("center.y", 
				                     UIInterpolatingMotionEffectType.TiltAlongVerticalAxis);				
			verticalEffect.MinimumRelativeValue = new NSNumber (-_motionEffectExtent);
			verticalEffect.MaximumRelativeValue = new NSNumber (_motionEffectExtent);

			var motionEffectGroup = new UIMotionEffectGroup ();
			motionEffectGroup.MotionEffects = new UIMotionEffect[] {
				horizontalEffect,
				verticalEffect
			};

			_dialogView.AddMotionEffect (motionEffectGroup);
		}

		SizeF CalculateDialogSize () {
			var dialogWidth = _containerView.Frame.Width;
			var dialogHeight = _containerView.Frame.Height + _buttonHeight + _buttonSpacerHeight;

			return new SizeF (dialogWidth, dialogHeight);
		}

		SizeF CalculateScreenSize () {
			if (_buttonTitles != null || _buttonTitles.Length > 0) {
				_buttonHeight = _defaultButtonHeight;
				_buttonSpacerHeight = _defaultButtonSpacerHeight;
			} else {
				_buttonHeight = 0;
				_buttonSpacerHeight = 0;
			}


			var screenWidth = UIScreen.MainScreen.Bounds.Width;
			var screenHeight = UIScreen.MainScreen.Bounds.Height;

			if (IsInterfaceOrientationLandscape ()) {
				var tmp = screenWidth;
				screenWidth = screenHeight;
				screenHeight = tmp;
			}

			return new SizeF (screenWidth, screenHeight);
		}

		#endregion

		#region Event handlers

		void CloseButtonTouchUpInside (object sender, EventArgs e) {
			var button = (UIButton)sender;

			if (ButtonTouched != null) {
				ButtonTouched (this, new ButtonTouchedEventArgs (button.Tag));
			}

			Close ();
		}

		bool IsInterfaceOrientationLandscape () {
			var interfaceOrientation = 
				UIApplication.SharedApplication.StatusBarOrientation;

			return interfaceOrientation == UIInterfaceOrientation.LandscapeLeft
			|| interfaceOrientation == UIInterfaceOrientation.LandscapeRight;
		}

		void KeyboardWillShowHandler (object sender, UIKeyboardEventArgs e) {
			var screenSize = CalculateScreenSize ();
			var dialogSize = CalculateDialogSize ();
			var keyboardSize = e.FrameBegin.Size;

			if (IsInterfaceOrientationLandscape ()) {
				var tmp = keyboardSize.Height;
				keyboardSize.Height = keyboardSize.Width;
				keyboardSize.Width = tmp;
			}

			UIView.Animate (.2f, 0f, UIViewAnimationOptions.TransitionNone, () => {
				var x = (screenSize.Width - dialogSize.Width) / 2;
				var y = (screenSize.Height - keyboardSize.Height - dialogSize.Height) / 2;
				var width = dialogSize.Width;
				var height = dialogSize.Height;

				_dialogView.Frame = new RectangleF (x, y, width, height);
			}, null);
		}

		void KeyboardWillHideHandler (object sender, UIKeyboardEventArgs e) {
			var screenSize = CalculateScreenSize ();
			var dialogSize = CalculateDialogSize ();

			UIView.Animate (.2f, 0f, UIViewAnimationOptions.TransitionNone, () => {
				var x = (screenSize.Width - dialogSize.Width) / 2;
				var y = (screenSize.Height - dialogSize.Height) / 2;
				var width = dialogSize.Width;
				var height = dialogSize.Height;

				_dialogView.Frame = new RectangleF (x, y, width, height);
			}, null);
		}

		void OrientationDidChangeHandler (object sender, EventArgs e) {
			if (_parentView == null) {
				return;
			}

			var interfaceOrientation = 
				UIApplication.SharedApplication.StatusBarOrientation;
			var startRotation = ValueForKeyPath (new NSString ("layer.transform.rotation.z")) as NSNumber;
			Console.WriteLine (startRotation);

			CGAffineTransform rotation;

			switch (interfaceOrientation) {
			case UIInterfaceOrientation.LandscapeLeft:
				rotation = CGAffineTransform.MakeRotation (-startRotation.FloatValue + (float)Math.PI * 270f / 180f);
				break;
			case UIInterfaceOrientation.LandscapeRight:
				rotation = CGAffineTransform.MakeRotation (-startRotation.FloatValue + (float)Math.PI * 90f / 180f);
				break;
			case UIInterfaceOrientation.PortraitUpsideDown:
				rotation = CGAffineTransform.MakeRotation (-startRotation.FloatValue + (float)Math.PI * 180f / 180f);
				break;
			default:
				rotation = CGAffineTransform.MakeRotation (-startRotation.FloatValue + 0f);
				break;
			}

			UIView.Animate (.2f, 0f, UIViewAnimationOptions.TransitionNone, () => {
				_dialogView.Transform = rotation;
			}, () => {
				/*
				// TODO: fix errors caused by being rotated one too many times

				dispatch_after(dispatch_time(DISPATCH_TIME_NOW, 0.5f * NSEC_PER_SEC), dispatch_get_main_queue(), ^{
					UIInterfaceOrientation endInterfaceOrientation = [[UIApplication sharedApplication] statusBarOrientation];
					if (interfaceOrientation != endInterfaceOrientation) {
						// TODO user moved phone again before than animation ended: rotation animation can introduce errors here
					}
				});
				*/

				var endInterfaceOrientation = 
					UIApplication.SharedApplication.StatusBarOrientation;
				if (interfaceOrientation != endInterfaceOrientation) {
					// TODO user moved phone again before than animation ended:
					//	rotation animation can introduce errors here
				}
			});
		}

		#endregion
	}

	public class ButtonTouchedEventArgs : EventArgs {
		public int ButtonIndex {
			get;
			private set;
		}

		public ButtonTouchedEventArgs (int buttonIndex) {
			ButtonIndex = buttonIndex;
		}
	}
}

