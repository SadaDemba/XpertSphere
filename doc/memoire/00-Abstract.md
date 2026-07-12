# Abstract

Recruitment is a strategic challenge for any organization, and the practices surrounding it are undergoing rapid transformation. In sub-Saharan Africa, and in Senegal in particular, this sector is characterized by a persistent paradox: a structurally growing demand for recruitment, combined with practices that remain largely manual and fragmented. The international solutions available on the market assume a level of digital maturity and financial resources that many local organizations have yet to achieve. Between these two realities, a space exists for a better-adapted solution.

This thesis presents the design and development of XpertSphere, a multi-tenant applicant tracking system (ATS) built to address the specific constraints of emerging African markets. The central research question focuses on the architectural and design choices that enable the development of such a platform while meeting both the technical requirements and the regulatory obligations of a professional HR tool.

The approach adopted is both analytical and applied. It draws on a contextual analysis of the recruitment landscape and existing solutions, a formalization of functional and non-functional requirements, and an architectural design structured around three axes: infrastructure sharing through a multi-tenant architecture, progressive adoption to accommodate organizations at varying stages of digital transition, and regulatory compliance with Senegalese data protection law and international standards.

On the technical side, XpertSphere is built on a .NET backend with SQL Server, two distinct Vue.js interfaces (one for candidates, one for internal users), and a Python service dedicated to automated CV analysis. The application is containerized with Docker, targeting Azure for cloud deployment.

The results demonstrate that a well-designed multi-tenant architecture can reconcile infrastructure cost-sharing, strict data isolation between organizations, and progressive adoption — opening concrete perspectives for the modernization of HR practices across sub-Saharan Africa.

---

**Keywords:** ATS, recruitment, multi-tenant, software architecture, emerging markets, Senegal, regulatory compliance, traceability, progressive adoption
