import NextAuth, { Profile } from "next-auth"
import { OIDCConfig } from "next-auth/providers"
import DuendeIDS6Provider from "next-auth/providers/duende-identity-server6"

// handlers – automatically create GET/POST routes for /api/auth/[...nextauth]
// signIn / signOut – functions you can call inside your React components
// auth – server-side helper to check the current session in Server Components or API routes, it can also be used as middleware (export { auth as middleware } from "@/auth";)
export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    DuendeIDS6Provider({
        // A unique identifier for this provider in your app.
        // Used internally by NextAuth for callback URLs, e.g. /api/auth/callback/id-server.
        id: "id-server",
        // Must match the ClientId you defined in your IdentityServer configuration
        clientId: "nextApp",
        // The same secret you defined in your IdentityServer Config
        // Acts like a password proving that this client is allowed to request tokens.
        clientSecret: "secret",
        // The base URL of your IdentityServer
        issuer: process.env.ID_URL,
        // describes how to request user login
        authorization: {
            // Defines what scopes to request when authenticating.
            params: { scope: "openid profile auctionApp" },
            // direct path to IdentityServer’s authorization endpoint
            url: process.env.ID_URL + "/connect/authorize"
        },
        // endpoint where NextAuth exchanges the authorization code for tokens (Access Token, ID Token)
        token: {
            // ID_URL_INTERNAL — an internal container address (e.g., within Docker network) for backend communication
            url: `${process.env.ID_URL_INTERNAL}/connect/token`,
        },
        // NextAuth fetch user claims (like name, email) from the identity provider
        userinfo: {
            url: `${process.env.ID_URL_INTERNAL}/connect/token`,
        },
        // Tells NextAuth to expect an ID Token in the response (standard in OIDC)
        // That token will contain user identity info and claims (like sub, name, email, etc.).
        // Useful for SPAs and SSR apps since it lets NextAuth easily derive the logged-in user session.
        idToken: true
    } as OIDCConfig<Omit<Profile, 'username'>>),
    // OIDCConfig is a generic interface in NextAuth that defines the expected shape of a configuration object for any OpenID Connect provider
    // OIDCConfig<Profile>: This OIDC provider returns a user profile that looks like the standard NextAuth Profile interface.
    // Omit<Type, Keys>: It creates a new type based on Type without the properties listed in Keys
    // Omit<Profile, 'username'>: Take the built-in NextAuth Profile type and remove the username field from it.
    // Why do we need to remove the username field? Because you’ve already extended the default Profile type elsewhere.
    // If you used OIDCConfig<Profile> directly, TypeScript would think your provider expects a username property coming from the identity provider itself, but in reality you’ll add it later in the callback (profile.username).
    // 
  ],
  // These two callbacks are the key to mapping user data between IdentityServer → JWT → NextAuth session.
  callbacks: {
    // Controls where users are redirected after sign-in/sign-out
    // If the URL begins with your site’s base (http://localhost:3000), allow it.
    // Otherwise, send them back to the home page.
    // url: the URL NextAuth is about to redirect the user to
    // baseUrl: your site’s root (from AUTH_URL or NEXTAUTH_URL, e.g. http://localhost:3000)
    // return value: where the user will actually be redirected after sign-in/sign-out
    // This callback is triggered automatically by NextAuth during specific transitions in the login/logout process, like after ssuccessful sign-in
    // When the user signs in via your provider (IdentityServer, Google, etc.),
    // NextAuth receives the authorization code → exchanges it for tokens → builds a session → then runs redirect() to decide where to send the user next.
    // Example flow:
    // 1. User clicks signIn('id-server').
    // 2. User logs in on IdentityServer.
    // 3. IdentityServer redirects back to /api/auth/callback/id-server.
    // 4. NextAuth finishes the flow, creates a session, and calls: redirect({ url: callbackUrl, baseUrl })
    //     - If you called signIn('id-server', { callbackUrl: '/session' }), then url will be /session.
    //     - The callback decides whether to allow it or override it.
    async redirect({ url, baseUrl }) {
        return url.startsWith(baseUrl) ? url : baseUrl;
    },
    // Runs in middleware when a protected route is matched
    async authorized({auth, request}) {
        const { pathname } = request.nextUrl

        if (pathname.startsWith("/_next") || pathname.startsWith("/favicon") || pathname.startsWith("/api/auth")) {
            return true;
        }

        if (pathname === '/session') {
            // !!: convert any value to boolean
            // auth is the session object. If the user is not logged in, auth will be null.
            // If a valid session (auth) exists → true → authorized, if no session → false → unauthorized
            return !!auth
        }
    },

    // Runs whenever a JWT is created or updated
    // profile contains user info from IdentityServer the first time the user signs in
    // jwt is the raw token stored in a cookie
    async jwt({ token, profile, account }) {
      if (account && account.access_token) {
          token.accessToken = account.access_token
      }
      if (profile) {
          // Add the username to the JWT
          token.username = profile.username
      }
      return token
    },
    // Runs whenever a session is created or accessed (e.g., via useSession() in React)
    // session is the decoded object you use in your app. accesss user info (session.user.name, etc.)
    async session({ session, token }) {
        if (token) {
            // copy data from the JWT into the user session object
            session.user.username = token.username;
            session.accessToken = token.accessToken; // send the access token to API
        }
        return session
    }
  },
  // custom pages
  pages: {
    signIn: '/signin',
  }
})


// The flow is:
// 1. User clicks “Login”.
// 2. NextAuth redirects to IdentityServer at http://localhost:5001/connect/authorize.
// 3. IdentityServer shows the login page.
// 4. After successful login, IdentityServer redirects back to: http://localhost:3000/api/auth/callback/id-server
// 5. NextAuth exchanges the authorization code for tokens (ID Token, Access Token).
// 6. NextAuth creates a session cookie for the user.
// 7. Your frontend can now access:
//     - session.user (user info)
//     - session.idToken (JWT from IdentityServer)
//     - session.accessToken (if configured)