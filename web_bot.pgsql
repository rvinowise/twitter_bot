--
-- PostgreSQL database dump
--

-- Dumped from database version 14.4
-- Dumped by pg_dump version 14.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: score_line; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.score_line (
    datetime timestamp without time zone NOT NULL,
    score integer,
    user_handle character varying(15) NOT NULL
);


ALTER TABLE public.score_line OWNER TO postgres;

--
-- Data for Name: score_line; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.score_line (datetime, score, user_handle) FROM stdin;
\.


--
-- Name: score_line score_line_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.score_line
    ADD CONSTRAINT score_line_pkey PRIMARY KEY (user_handle, datetime);


--
-- PostgreSQL database dump complete
--

