"use client";

import { useSession } from "next-auth/react";
import LoginButton from "./LoginButton";
import Logo from "./Logo";
import Search from "./Search";
import UserActions from "./UserActions";

export default function NavBar() {
  const session = useSession();

  return (
    <header className="sticky top-0 z-50 flex justify-between bg-white p-5 items-center text-gray-800 shadow-md">
      <Logo />
      <Search />
      {session.data?.user ? (
        <UserActions user={session.data.user} />
      ) : (
        <LoginButton />
      )}
    </header>
  );
}
