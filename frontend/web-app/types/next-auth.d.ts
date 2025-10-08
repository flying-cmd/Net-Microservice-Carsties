import { type DefaultSession } from "next-auth" // DefaultSession defines the default structure of a user session
import { type JWT } from "next-auth/jwt"
 
// add extra type definitions to the existing next-auth module
// in ts, when you declare another interface with the same name in the same scope(or in a declare module block), ts merges them
declare module "next-auth" {
  /**
   * Returned by `auth`, `useSession`, `getSession` and received as a prop on the `SessionProvider` React Context
   */
  // Combines your custom field (username) with the default fields (name, email, image). The & means intersection type — it merges both sets of properties.
  interface Session {
    user: {
      /** The user's postal address. */
      // when access session.user.username
      username: string
      /**
       * By default, TypeScript merges new interface properties and overwrites existing ones.
       * In this case, the default session user properties will be overwritten,
       * with the new ones defined above. To keep the default session user properties,
       * you need to add them back into the newly declared interface.
       */
    } & DefaultSession["user"];
    accessToken: string
  }
  // Your session’s user object now looks like this in TypeScript:
  // {
  //   username: string
  //   name?: string | null
  //   email?: string | null
  //   image?: string | null
  // }

  interface Profile {
    // when access profile.username
    username: string
  }

  interface User {
    username: string
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    // the object returned in jwt callback
    username: string;
    accessToken: string;
  }
}