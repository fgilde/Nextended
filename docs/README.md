# Nextended Documentation

Welcome to the Nextended documentation!

## 🌐 Online Documentation

The documentation is available online at: **https://fgilde.github.io/Nextended/**

## 📁 Documentation Structure

```
docs/
├── index.md                    # Main documentation portal
├── _config.yml                 # GitHub Pages configuration
│
├── guides/                     # User guides
│   ├── installation.md         # Installation and setup guide
│   ├── architecture.md         # Solution architecture overview
│   └── migration.md            # Migration from nExt to Nextended
│
├── projects/                   # Project-specific documentation
│   ├── README.md               # Projects overview
│   ├── core.md                 # Nextended.Core documentation
│   ├── blazor.md               # Nextended.Blazor documentation
│   ├── cache.md                # Nextended.Cache documentation
│   ├── ef.md                   # Nextended.EF documentation
│   ├── imaging.md              # Nextended.Imaging documentation
│   ├── ui.md                   # Nextended.UI documentation
│   ├── web.md                  # Nextended.Web documentation
│   ├── aspire.md               # Nextended.Aspire documentation
│   ├── codegen.md              # Nextended.CodeGen documentation
│   └── autodto.md              # Nextended.AutoDto documentation
│
├── examples/                   # Usage examples
│   └── common-use-cases.md     # Real-world examples and use cases
│
└── api/                        # API reference
    ├── extensions.md           # Extension methods reference
    ├── types.md                # Custom types reference
    ├── class-mapping.md        # Class mapping reference
    ├── helpers.md              # Helper utilities reference
    └── encryption.md           # Encryption and security reference
```

## 📖 Quick Links

- [Installation Guide](guides/installation.md)
- [Architecture Overview](guides/architecture.md)
- [All Projects](projects/README.md)
- [Common Use Cases](examples/common-use-cases.md)
- [Extension Methods API](api/extensions.md)
- [Custom Types API](api/types.md)
- [Class Mapping API](api/class-mapping.md)
- [Helper Utilities API](api/helpers.md)
- [Encryption & Security API](api/encryption.md)

## 🚀 GitHub Pages Setup

The documentation is automatically deployed to GitHub Pages using GitHub Actions.

### Setup Instructions

1. **Enable GitHub Pages** in repository settings:
   - Go to Settings → Pages
   - Source: GitHub Actions
   - The workflow in `.github/workflows/pages.yml` will handle deployment

2. **Automatic Deployment**:
   - Documentation is deployed automatically when changes are pushed to the `main` branch
   - Only changes to the `docs/` folder trigger deployment
   - Manual deployment can be triggered via the Actions tab

3. **Jekyll Theme**:
   - Theme: `jekyll-theme-cayman`
   - Configured in `_config.yml`

## 🤝 Contributing to Documentation

To improve the documentation:

1. Edit the relevant `.md` files in the `docs/` folder
2. Preview locally using Jekyll (optional):
   ```bash
   cd docs
   bundle exec jekyll serve
   ```
3. Commit and push changes
4. Documentation will be automatically deployed

## 📝 Documentation Standards

- Use clear, concise language
- Include code examples where appropriate
- Link to related documentation
- Follow the existing structure and style
- Test all code examples before committing

## 🔗 External Links

- [Main Repository](https://github.com/fgilde/Nextended)
- [NuGet Packages](https://www.nuget.org/packages?q=Nextended)
- [Report Issues](https://github.com/fgilde/Nextended/issues)
