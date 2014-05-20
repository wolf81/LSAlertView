LSAlertView
===========

Customizable alert view replacement for iOS, written in C#. Code is based on the Objective-C version at: https://github.com/wimagguc/ios-custom-alertview

How to use it? 

1. Create a new LSAlertView
2. Create and set a subview that is to be contained within the alert view
3. Add the event handler. Don't forget to decouble the event handler when button is touched.

Setting up a new alert view:

    var alertView = new LSAlertView ();
    var remarkView = new EditRemarkView (); // a custom subclass of UIView
    alertView.ContainerView = remarkView; 
    alertView.ButtonTitles = new string[] { "Cancel", "Save" };
    alertView.Show ();
    alertView.ButtonTouched += HandleButtonTouched;

    // in this example my custom UIView subclass has some properties I can set
    remarkView.Title = "Remark";
    remarkView.Remarks = projectItem.Remarks;
			
    // I want to display the keyboard when the alert view appears, since my custom
    // UIView subclass contains a text field
    remarkView.BecomeFirstResponder ();
			
The event handler that is being executed on button touch:			

    // the event handler
    void HandleButtonTouched (object sender, ButtonTouchedEventArgs e) {
      alertView.ButtonTouched -= HandleButtonTouched; // decouple event handler

      // in this example the first button is just a cancel button
      if (e.ButtonIndex == 0) {
			  return;
      }

      var alertView = sender as LSAlertView;
      // typecast the container view to it's original type, so we can access it's properties
      var remarkView = alertView.ContainerView as EditRemarkView; 
      var text = remarkView.Remarks;

      text = (text != null) ? 
        text.Trim ().Length == 0 ? null : text.Trim () : null;

      var projectItem = ProjectItems [CurrentProjectIndex];
      projectItem.Remarks = text;
      var service = Manager.Instance;
      service.UpdateProjectItem (projectItem);

      _currentViewController.UpdateCaptionText (text);
    }			

---

There's some room for improvements:

- use drawing methods to draw seperator lines, as the current seperator lines (created by UIViews) for buttons are a bit too "thick" due to shadows and the like.
- probably other aspects as well :)

