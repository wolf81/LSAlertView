using System;
using System.Drawing;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace Depot.iOS {
	public class LSAlertViewButton : UIButton {
		public LSAlertViewButton (UIButtonType type) : base (type) {
			SetTitleColor (new UIColor (0f, .5f, 1f, 1f), UIControlState.Normal);

			SetBackgroundImage (CreateColorImage (new UIColor (217f / 255f, 217f / 255f, 217f / 255f, 1f)), UIControlState.Highlighted);
		}

		UIImage CreateColorImage (UIColor color) {
			var rect = new RectangleF (0.0f, 0.0f, 1.0f, 1.0f);

			UIGraphics.BeginImageContext (rect.Size);
			var context = UIGraphics.GetCurrentContext ();
			context.SetFillColor (color.CGColor);
			context.FillRect (rect);
			var image = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();

			return image;
		}
	}
}

