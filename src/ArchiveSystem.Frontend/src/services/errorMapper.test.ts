import { AxiosError, type AxiosResponse } from "axios";
import { describe, expect, it } from "vitest";
import {
  caseDynamicFieldsForbiddenMessage,
  documentCreateForbiddenMessage,
  mapApiError,
  readOnlyClassificationMessage,
} from "@/services/errorMapper";

describe("mapApiError", () => {
  it("maps generic validation failures to Persian", () => {
    const error = new AxiosError("Request failed");
    error.response = {
      status: 400,
      statusText: "Bad Request",
      data: { detail: "One or more validation failures have occurred." },
      headers: {},
      config: {},
    } as AxiosResponse;

    expect(mapApiError(error).message).toBe(
      "یک یا چند خطای اعتبارسنجی رخ داده است.",
    );
  });

  it.each([
    "User is not allowed to upload to this case.",
    "User is not allowed to upload to this classification.",
  ])("maps document upload permission errors to Persian", (detail) => {
    const error = new AxiosError("Request failed");
    error.response = {
      status: 403,
      statusText: "Forbidden",
      data: { detail },
      headers: {},
      config: {},
    } as AxiosResponse;

    expect(mapApiError(error).message).toBe(documentCreateForbiddenMessage);
  });

  it.each([
    "User is not allowed to modify this file.",
    "User is not allowed to modify this upload session.",
  ])("maps read-only ACL errors to a clear Persian message", (detail) => {
    const error = new AxiosError("Request failed");
    error.response = {
      status: 403,
      statusText: "Forbidden",
      data: { detail },
      headers: {},
      config: {},
    } as AxiosResponse;

    expect(mapApiError(error).message).toBe(readOnlyClassificationMessage);
  });

  it.each([
    "You are not allowed to read case dynamic fields.",
    "You are not allowed to write case dynamic fields.",
    "You are not allowed to modify case dynamic fields.",
  ])("maps case dynamic-field permission errors to Persian", (detail) => {
    const error = new AxiosError("Request failed");
    error.response = {
      status: 403,
      statusText: "Forbidden",
      data: { detail },
      headers: {},
      config: {},
    } as AxiosResponse;

    expect(mapApiError(error).message).toBe(
      caseDynamicFieldsForbiddenMessage,
    );
  });
});
