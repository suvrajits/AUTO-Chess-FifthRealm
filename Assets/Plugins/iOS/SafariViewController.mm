#import <SafariServices/SafariServices.h>

extern "C"
{
@interface SafariViewController : UIViewController<SFSafariViewControllerDelegate>
@end

@implementation SafariViewController
- (void)safariViewController:(SFSafariViewController *)controller didCompleteInitialLoad:(BOOL)didLoadSuccessfully
{
    NSLog(@"didCompleteInitialLoad: %d", didLoadSuccessfully);
    UnitySendMessage("SafariViewController", "didCompleteInitialLoad", didLoadSuccessfully ? "1" : "0");
}

- (void)safariViewController:(SFSafariViewController *)controller initialLoadDidRedirectToURL:(NSURL *)URL
{
    NSLog(@"initialLoadDidRedirectToURL: %@", URL.absoluteString);
    UnitySendMessage("SafariViewController", "initialLoadDidRedirectToURL", [URL.absoluteString UTF8String]);
}

- (void)safariViewControllerDidFinish:(SFSafariViewController *)controller
{
    NSLog(@"safariViewControllerDidFinish");
    UnitySendMessage("SafariViewController", "didFinish", "");
}

@end

SafariViewController * svc;

void openURL(const char * url)
{
    NSLog(@"Launching SFSafariViewController with URL=%@", [[NSString alloc] initWithUTF8String:url]);
    UIViewController * uvc = UnityGetGLViewController();
    NSURL * URL = [NSURL URLWithString: [[NSString alloc] initWithUTF8String:url]];
    SFSafariViewController * sfvc = [[SFSafariViewController alloc] initWithURL:URL];
    svc = [[SafariViewController alloc] init];
    sfvc.delegate = svc;
    [uvc presentViewController:sfvc animated:YES completion:nil];
}

void dismiss()
{
    UIViewController * uvc = UnityGetGLViewController();
    [uvc dismissViewControllerAnimated:YES completion:nil];
}
}
