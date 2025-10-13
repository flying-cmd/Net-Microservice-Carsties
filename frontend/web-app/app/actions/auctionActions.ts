"use server";

import { Auction, Bid, PagedResult } from "@/types";
import { fetchWrapper } from "../lib/fetchWarpper";
import { FieldValues } from "react-hook-form";

export async function getData(query: string): Promise<PagedResult<Auction>> {
  return await fetchWrapper.get(`search${query}`);
}

export async function updateAuctionTest(): Promise<{status: number, message: string}> {
    const data = {
        mileage: Math.floor(Math.random() * 100000) + 1,
    }

    return await fetchWrapper.put(`auctions/afbee524-5972-4075-8800-7d1f9d7b0a0c`, data);
}

export async function createAuction(data: FieldValues) {
  return await fetchWrapper.post("auctions", data);
}

export async function getDetaildViewData(id: string): Promise<Auction> {
  return await fetchWrapper.get(`auctions/${id}`);
}

export async function updateAuction(data: FieldValues, id: string) {
  return await fetchWrapper.put(`auctions/${id}`, data);
}

export async function deleteAuction(id: string) {
  return await fetchWrapper.del(`auctions/${id}`);
}

export async function getBidsForAuction(id: string): Promise<Bid[]> {
  return await fetchWrapper.get(`bids/${id}`);
}

export async function placeBidForAuction(auctionId: string, amount: number) {
    return await fetchWrapper.post(`bids?auctionId=${auctionId}&amount=${amount}`, {});
}