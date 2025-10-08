import NextAuth, { Profile } from "next-auth"
import { OIDCConfig } from "next-auth/providers"
import DuendeIDS6Provider from "next-auth/providers/duende-identity-server6"

// handlers – automatically create GET/POST routes for /api/auth/[...nextauth]
// signIn / signOut – functions you can call inside your React components
// auth – server-side helper to check the current session in Server Components or API routes
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
        issuer: "http://localhost:5001",
        // Defines what scopes to request when authenticating.
        authorization: { params: { scope: "openid profile auctionApp" } },
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
    async authorized({auth}) {
        // !!: convert any value to boolean
        // auth is the session object. If the user is not logged in, auth will be null.
        // If a valid session (auth) exists → true → authorized, if no session → false → unauthorized
        return !!auth
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